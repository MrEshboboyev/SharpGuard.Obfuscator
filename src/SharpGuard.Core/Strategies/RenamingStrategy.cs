using dnlib.DotNet;
using SharpGuard.Core.Abstractions;
using SharpGuard.Core.Configuration;
using SharpGuard.Core.Services;
using System.Collections.Immutable;
using System.Globalization;
using ILogger = SharpGuard.Core.Services.ILogger;

namespace SharpGuard.Core.Strategies;

/// <summary>
/// Advanced renaming strategy with contextual awareness and collision avoidance
/// Implements Flyweight and Factory patterns
/// </summary>
public class RenamingStrategy(
    IRandomGenerator random,
    ILogger logger
) : IProtectionStrategy
{
    public string Id => "renaming";
    public string Name => "Intelligent Symbol Renaming";
    public string Description => "Renames symbols with collision-free, context-aware obfuscation";
    public int Priority => 700;
    public ImmutableArray<string> Dependencies => [];
    public ImmutableArray<string> ConflictsWith => [];

    private readonly NameGenerator _nameGenerator = new(random);
    private readonly Dictionary<string, string> _renameMap = [];
    private readonly HashSet<string> _usedNames = [];

    public bool CanApply(ModuleDef module)
    {
        return module.Types.Count > 1; // Skip modules with only module type
    }

    public void Apply(ModuleDef module, ProtectionContext context)
    {
        if (!context.Configuration.Renaming.Enabled) return;

        var config = context.Configuration.Renaming;
        var renamedItems = 0;

        // Analyze naming context first
        var namingContext = AnalyzeNamingContext(module, context);

        // Rename types
        if (config.Mode >= RenamingMode.Light)
        {
            renamedItems += RenameTypes(module, namingContext, config, context);
        }

        // Rename members
        if (config.Mode >= RenamingMode.Normal)
        {
            renamedItems += RenameMembers(module, namingContext, config, context);
        }

        // Handle cross-references and generics
        ResolveCrossReferences(module, context);

        // Generate mapping file if requested
        if (config.GenerateMappingFile)
        {
            GenerateMappingFile(context);
        }

        logger.LogInformation("Renaming: {Count} symbols renamed", renamedItems);
    }

    private NamingContext AnalyzeNamingContext(ModuleDef module, ProtectionContext context)
    {
        var namingContext = new NamingContext();
        
        // Collect existing names to avoid collisions
        foreach (var type in module.GetTypes())
        {
            namingContext.ExistingNames.Add(type.Name.String);
            
            foreach (var method in type.Methods)
            {
                namingContext.ExistingNames.Add(method.Name.String);
            }
            
            foreach (var field in type.Fields)
            {
                namingContext.ExistingNames.Add(field.Name.String);
            }
            
            foreach (var prop in type.Properties)
            {
                namingContext.ExistingNames.Add(prop.Name.String);
            }
        }

        // Identify framework types that shouldn't be renamed
        namingContext.FrameworkTypes = IdentifyFrameworkTypes(module);

        return namingContext;
    }

    private int RenameTypes(ModuleDef module, NamingContext namingContext, RenamingOptions config, ProtectionContext context)
    {
        var renamed = 0;
        
        foreach (var type in module.GetTypes().Where(t => ShouldRenameType(t, config, context)))
        {
            var newName = GenerateTypeName(type, namingContext, config);
            if (newName != type.Name.String)
            {
                _renameMap[type.FullName] = newName;
                type.Name = newName;
                _usedNames.Add(newName);
                renamed++;
                
                context.AddDiagnostic(DiagnosticSeverity.Info, "RN001", 
                    $"Renamed type {type.Name} to {newName}");
            }
        }
        
        return renamed;
    }

    private int RenameMembers(ModuleDef module, NamingContext namingContext, RenamingOptions config, ProtectionContext context)
    {
        var renamed = 0;
        
        foreach (var type in module.GetTypes())
        {
            if (IsExcludedType(type, context)) continue;

            // Rename methods
            if (config.RenameProperties)
            {
                foreach (var method in type.Methods.Where(m => ShouldRenameMethod(m, config, context)))
                {
                    var newName = GenerateMethodName(method, namingContext, config);
                    if (newName != method.Name.String)
                    {
                        _renameMap[method.FullName] = newName;
                        method.Name = newName;
                        _usedNames.Add(newName);
                        renamed++;
                    }
                }
            }

            // Rename fields
            if (config.RenameFields)
            {
                foreach (var field in type.Fields.Where(f => ShouldRenameField(f, config, context)))
                {
                    var newName = GenerateFieldName(field, namingContext, config);
                    if (newName != field.Name.String)
                    {
                        _renameMap[field.FullName] = newName;
                        field.Name = newName;
                        _usedNames.Add(newName);
                        renamed++;
                    }
                }
            }

            // Rename properties
            if (config.RenameProperties)
            {
                foreach (var prop in type.Properties.Where(p => ShouldRenameProperty(p, config, context)))
                {
                    var newName = GeneratePropertyName(prop, namingContext, config);
                    if (newName != prop.Name.String)
                    {
                        _renameMap[prop.FullName] = newName;
                        prop.Name = newName;
                        _usedNames.Add(newName);
                        renamed++;
                        
                        // Also rename getter/setter methods
                        RenamePropertyAccessors(prop, newName);
                    }
                }
            }

            // Rename events
            if (config.RenameEvents)
            {
                foreach (var evt in type.Events.Where(e => ShouldRenameEvent(e, config, context)))
                {
                    var newName = GenerateEventName(evt, namingContext, config);
                    if (newName != evt.Name.String)
                    {
                        _renameMap[evt.FullName] = newName;
                        evt.Name = newName;
                        _usedNames.Add(newName);
                        renamed++;
                        
                        // Rename event handler methods
                        RenameEventHandlers(evt, newName);
                    }
                }
            }
        }
        
        return renamed;
    }

    private void ResolveCrossReferences(ModuleDef module, ProtectionContext context)
    {
        // Fix references in method bodies
        foreach (var type in module.GetTypes())
        {
            foreach (var method in type.Methods.Where(m => m.HasBody))
            {
                FixMethodReferences(method, context);
            }
        }

        // Fix custom attributes
        FixCustomAttributes(module, context);

        // Fix XML documentation references
        FixDocumentationReferences(module, context);
    }

    private string GenerateTypeName(TypeDef type, NamingContext context, RenamingOptions config)
    {
        if (context.FrameworkTypes.Contains(type.FullName))
            return type.Name.String;

        return config.Mode switch
        {
            RenamingMode.None => type.Name.String,
            RenamingMode.Light => _nameGenerator.GenerateSimpleTypeName(type),
            RenamingMode.Normal => _nameGenerator.GenerateNormalTypeName(type, context),
            RenamingMode.Aggressive => _nameGenerator.GenerateAggressiveTypeName(type, context, config),
            _ => type.Name.String
        };
    }

    private string GenerateMethodName(MethodDef method, NamingContext context, RenamingOptions config)
    {
        // Preserve special method names
        if (method.IsConstructor || method.IsStaticConstructor)
            return method.Name.String;

        // Preserve operator overloads
        if (method.IsSpecialName && method.Name.StartsWith("op_"))
            return method.Name.String;

        // Preserve interface implementation methods if public API preservation is enabled
        if (method.IsVirtual && method.Overrides.Count > 0)
            return method.Name.String;

        return config.Mode switch
        {
            RenamingMode.None => method.Name.String,
            RenamingMode.Light => _nameGenerator.GenerateSimpleMethodName(method),
            RenamingMode.Normal => _nameGenerator.GenerateNormalMethodName(method, context),
            RenamingMode.Aggressive => _nameGenerator.GenerateAggressiveMethodName(method, context),
            _ => method.Name.String
        };
    }

    private string GenerateFieldName(FieldDef field, NamingContext context, RenamingOptions config)
    {
        return config.Mode switch
        {
            RenamingMode.None => field.Name.String,
            RenamingMode.Light => _nameGenerator.GenerateSimpleFieldName(field),
            RenamingMode.Normal => _nameGenerator.GenerateNormalFieldName(field, context),
            RenamingMode.Aggressive => _nameGenerator.GenerateAggressiveFieldName(field, context),
            _ => field.Name.String
        };
    }

    private string GeneratePropertyName(PropertyDef property, NamingContext context, RenamingOptions config)
    {
        return config.Mode switch
        {
            RenamingMode.None => property.Name.String,
            RenamingMode.Light => _nameGenerator.GenerateSimplePropertyName(property),
            RenamingMode.Normal => _nameGenerator.GenerateNormalPropertyName(property, context),
            RenamingMode.Aggressive => _nameGenerator.GenerateAggressivePropertyName(property, context),
            _ => property.Name.String
        };
    }

    private string GenerateEventName(EventDef evt, NamingContext context, RenamingOptions config)
    {
        return config.Mode switch
        {
            RenamingMode.None => evt.Name.String,
            RenamingMode.Light => _nameGenerator.GenerateSimpleEventName(evt),
            RenamingMode.Normal => _nameGenerator.GenerateNormalEventName(evt, context),
            RenamingMode.Aggressive => _nameGenerator.GenerateAggressiveEventName(evt, context),
            _ => evt.Name.String
        };
    }

    private void RenamePropertyAccessors(PropertyDef property, string newName)
    {
        if (property.GetMethod != null)
        {
            var getterName = "get_" + newName;
            _renameMap[property.GetMethod.FullName] = getterName;
            property.GetMethod.Name = getterName;
        }

        if (property.SetMethod != null)
        {
            var setterName = "set_" + newName;
            _renameMap[property.SetMethod.FullName] = setterName;
            property.SetMethod.Name = setterName;
        }
    }

    private void RenameEventHandlers(EventDef evt, string newName)
    {
        if (evt.AddMethod != null)
        {
            var addName = "add_" + newName;
            _renameMap[evt.AddMethod.FullName] = addName;
            evt.AddMethod.Name = addName;
        }

        if (evt.RemoveMethod != null)
        {
            var removeName = "remove_" + newName;
            _renameMap[evt.RemoveMethod.FullName] = removeName;
            evt.RemoveMethod.Name = removeName;
        }

        if (evt.InvokeMethod != null)
        {
            var raiseName = "raise_" + newName;
            _renameMap[evt.InvokeMethod.FullName] = raiseName;
            evt.InvokeMethod.Name = raiseName;
        }
    }

    private bool ShouldRenameType(TypeDef type, RenamingOptions config, ProtectionContext context)
    {
        // Don't rename if excluded
        if (IsExcludedType(type, context))
            return false;

        // Don't rename special types
        if (type.IsGlobalModuleType || type.IsSpecialName)
            return false;

        // Don't rename public types if preserving public API
        if (type.IsPublic && context.Configuration.PreservePublicApi)
            return false;

        // Don't rename framework types
        if (type.FullName.StartsWith("System.") || 
            type.FullName.StartsWith("Microsoft.") ||
            type.FullName.StartsWith("Windows."))
            return false;

        return true;
    }

    private bool ShouldRenameMethod(MethodDef method, RenamingOptions config, ProtectionContext context)
    {
        // Don't rename if excluded
        if (context.Configuration.ExcludedMethods.Contains(method.FullName))
            return false;

        // Don't rename constructors
        if (method.IsConstructor || method.IsStaticConstructor)
            return false;

        // Don't rename special methods
        if (method.IsSpecialName)
            return false;

        // Don't rename public methods if preserving public API
        if ((method.IsPublic || method.IsFamily) && context.Configuration.PreservePublicApi)
            return false;

        // Don't rename interface implementations
        if (method.Overrides.Count > 0)
            return false;

        // Don't rename P/Invoke methods
        if (method.IsPinvokeImpl)
            return false;

        return true;
    }

    private bool ShouldRenameField(FieldDef field, RenamingOptions config, ProtectionContext context)
    {
        // Don't rename if excluded
        if (context.Configuration.ExcludedMethods.Contains(field.FullName))
            return false;

        // Don't rename special fields
        if (field.IsSpecialName)
            return false;

        // Don't rename literal fields (const)
        if (field.IsLiteral)
            return false;

        // Don't rename public fields if preserving public API
        if (field.IsPublic && context.Configuration.PreservePublicApi)
            return false;

        return true;
    }

    private bool ShouldRenameProperty(PropertyDef property, RenamingOptions config, ProtectionContext context)
    {
        // Properties inherit renaming rules from their accessors
        var shouldRenameGetter = property.GetMethod != null && ShouldRenameMethod(property.GetMethod, config, context);
        var shouldRenameSetter = property.SetMethod != null && ShouldRenameMethod(property.SetMethod, config, context);
        
        return shouldRenameGetter || shouldRenameSetter;
    }

    private bool ShouldRenameEvent(EventDef evt, RenamingOptions config, ProtectionContext context)
    {
        // Events inherit renaming rules from their handlers
        var shouldRenameAdd = evt.AddMethod != null && ShouldRenameMethod(evt.AddMethod, config, context);
        var shouldRenameRemove = evt.RemoveMethod != null && ShouldRenameMethod(evt.RemoveMethod, config, context);
        
        return shouldRenameAdd || shouldRenameRemove;
    }

    private bool IsExcludedType(TypeDef type, ProtectionContext context)
    {
        return context.Configuration.ExcludedNamespaces.Contains(type.Namespace) ||
               context.Configuration.ExcludedTypes.Contains(type.FullName);
    }

    private HashSet<string> IdentifyFrameworkTypes(ModuleDef module)
    {
        var frameworkTypes = new HashSet<string>();
        
        // Add common framework type prefixes
        var prefixes = new[]
        {
            "System.",
            "Microsoft.",
            "Windows.",
            "corlib",
            "mscorlib"
        };

        foreach (var type in module.GetTypes())
        {
            if (prefixes.Any(prefix => type.FullName.StartsWith(prefix, StringComparison.Ordinal)))
            {
                frameworkTypes.Add(type.FullName);
            }
        }

        return frameworkTypes;
    }

    private void FixMethodReferences(MethodDef method, ProtectionContext context)
    {
        // Implementation would fix references in IL code
    }

    private void FixCustomAttributes(ModuleDef module, ProtectionContext context)
    {
        // Implementation would fix attribute references
    }

    private void FixDocumentationReferences(ModuleDef module, ProtectionContext context)
    {
        // Implementation would fix XML doc references
    }

    private void GenerateMappingFile(ProtectionContext context)
    {
        // Implementation would generate a mapping file for deobfuscation
    }
}


internal class NamingContext
{
    public HashSet<string> ExistingNames { get; } = new();
    public HashSet<string> FrameworkTypes { get; set; } = new();
}

/// <summary>
/// Generates obfuscated names using various algorithms
/// Implements Strategy pattern for name generation
/// </summary>
internal class NameGenerator
{
    private readonly IRandomGenerator _random;
    private readonly char[] _baseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
    private readonly string[] _dictionaryWords = {
        "Abstract", "Adapter", "Bridge", "Builder", "Chain", "Command", "Composite", "Decorator",
        "Facade", "Factory", "Flyweight", "Interpreter", "Iterator", "Mediator", "Memento",
        "Observer", "Prototype", "Proxy", "Singleton", "State", "Strategy", "Template", "Visitor"
    };

    public NameGenerator(IRandomGenerator random)
    {
        _random = random;
    }

    public string GenerateSimpleTypeName(TypeDef type)
    {
        return GenerateRandomString(8, 12);
    }

    public string GenerateNormalTypeName(TypeDef type, NamingContext context)
    {
        var baseName = _dictionaryWords[_random.Next(0, _dictionaryWords.Length)];
        var suffix = GenerateRandomString(3, 6);
        var fullName = baseName + suffix;
        
        return EnsureUnique(fullName, context);
    }

    public string GenerateAggressiveTypeName(TypeDef type, NamingContext context, RenamingOptions config)
    {
        if (config.FlattenNamespaces)
        {
            return GenerateFlattenedName(type, context);
        }
        
        var prefix = config.NamespacePrefix;
        var randomPart = GenerateRandomString(10, 15);
        var fullName = prefix + randomPart;
        
        return EnsureUnique(fullName, context);
    }

    public string GenerateSimpleMethodName(MethodDef method)
    {
        return "_" + GenerateRandomString(6, 10);
    }

    public string GenerateNormalMethodName(MethodDef method, NamingContext context)
    {
        var verb = GetMethodVerb(method);
        var noun = GetMethodNoun(method);
        var fullName = verb + noun + GenerateRandomString(2, 4);
        
        return EnsureUnique(fullName, context);
    }

    public string GenerateAggressiveMethodName(MethodDef method, NamingContext context)
    {
        return GenerateRandomString(15, 25);
    }

    public string GenerateSimpleFieldName(FieldDef field)
    {
        return "_" + GenerateRandomString(4, 8);
    }

    public string GenerateNormalFieldName(FieldDef field, NamingContext context)
    {
        var prefix = field.IsPrivate ? "_" : "";
        var baseName = GetFieldBaseName(field);
        var fullName = prefix + baseName + GenerateRandomString(2, 4);
        
        return EnsureUnique(fullName, context);
    }

    public string GenerateAggressiveFieldName(FieldDef field, NamingContext context)
    {
        var prefix = field.IsPrivate ? "_" : "";
        var fullName = prefix + GenerateRandomString(8, 16);
        
        return EnsureUnique(fullName, context);
    }

    public string GenerateSimplePropertyName(PropertyDef property)
    {
        return GenerateRandomString(6, 10);
    }

    public string GenerateNormalPropertyName(PropertyDef property, NamingContext context)
    {
        var baseName = GetPropertyBaseName(property);
        var fullName = baseName + GenerateRandomString(2, 4);
        
        return EnsureUnique(fullName, context);
    }

    public string GenerateAggressivePropertyName(PropertyDef property, NamingContext context)
    {
        return GenerateRandomString(12, 20);
    }

    public string GenerateSimpleEventName(EventDef evt)
    {
        return "On" + GenerateRandomString(6, 10);
    }

    public string GenerateNormalEventName(EventDef evt, NamingContext context)
    {
        var baseName = GetEventBaseName(evt);
        var fullName = "On" + baseName + GenerateRandomString(2, 4);
        
        return EnsureUnique(fullName, context);
    }

    public string GenerateAggressiveEventName(EventDef evt, NamingContext context)
    {
        return "On" + GenerateRandomString(10, 18);
    }

    private string GenerateRandomString(int minLength, int maxLength)
    {
        var length = _random.Next(minLength, maxLength + 1);
        var chars = new char[length];
        
        for (int i = 0; i < length; i++)
        {
            chars[i] = _baseChars[_random.Next(0, _baseChars.Length)];
        }
        
        return new string(chars);
    }

    private string GenerateFlattenedName(TypeDef type, NamingContext context)
    {
        var parts = type.FullName.Split('.');
        var flattened = string.Concat(parts.Select(p => p.Length > 0 ? p[0].ToString() : ""));
        var suffix = GenerateRandomString(8, 12);
        var fullName = flattened + suffix;
        
        return EnsureUnique(fullName, context);
    }

    private string GetMethodVerb(MethodDef method)
    {
        var name = method.Name.String.ToLowerInvariant();
        
        if (name.StartsWith("get")) return "Get";
        if (name.StartsWith("set")) return "Set";
        if (name.StartsWith("add")) return "Add";
        if (name.StartsWith("remove")) return "Remove";
        if (name.StartsWith("create")) return "Create";
        if (name.StartsWith("initialize")) return "Init";
        if (name.StartsWith("process")) return "Proc";
        if (name.StartsWith("calculate")) return "Calc";
        
        return "Do";
    }

    private string GetMethodNoun(MethodDef method)
    {
        // Extract meaningful noun from method name
        var name = method.Name.String;
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.Substring(Math.Min(3, name.Length)));
    }

    private string GetFieldBaseName(FieldDef field)
    {
        var typeName = field.FieldType.FullName;
        return typeName.Split('.').Last().Replace("+", "");
    }

    private string GetPropertyBaseName(PropertyDef property)
    {
        return property.Name.String;
    }

    private string GetEventBaseName(EventDef evt)
    {
        return evt.Name.String;
    }

    private string EnsureUnique(string name, NamingContext context)
    {
        var uniqueName = name;
        var counter = 1;
        
        while (context.ExistingNames.Contains(uniqueName))
        {
            uniqueName = name + counter.ToString(CultureInfo.InvariantCulture);
            counter++;
        }
        
        context.ExistingNames.Add(uniqueName);
        return uniqueName;
    }
}
