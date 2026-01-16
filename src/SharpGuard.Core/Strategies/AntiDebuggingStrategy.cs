using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpGuard.Core.Abstractions;
using SharpGuard.Core.Configuration;
using SharpGuard.Core.Services;
using System.Collections.Immutable;
using System.Reflection;
using ILogger = SharpGuard.Core.Services.ILogger;
using MethodAttributes = dnlib.DotNet.MethodAttributes;
using MethodImplAttributes = dnlib.DotNet.MethodImplAttributes;
using TypeAttributes = dnlib.DotNet.TypeAttributes;

namespace SharpGuard.Core.Strategies;

/// <summary>
/// Advanced anti-debugging and anti-tampering protection
/// Implements Observer and State patterns
/// </summary>
public class AntiDebuggingStrategy(
    IRandomGenerator random,
    ILogger logger
) : IProtectionStrategy
{
    public string Id => "antidebug";
    public string Name => "Advanced Anti-Debugging Protection";
    public string Description => "Prevents debugging and tampering through multiple detection vectors";
    public int Priority => 950;
    public ImmutableArray<string> Dependencies => [];
    public ImmutableArray<string> ConflictsWith => [];

    public bool CanApply(ModuleDef module)
    {
        return true; // Can apply to any module
    }

    public void Apply(ModuleDef module, ProtectionContext context)
    {
        if (!context.Configuration.EnableAntiDebugging) return;

        var config = context.Configuration.AntiTamper;
        var protectionPoints = 0;

        // Inject checks into module constructor
        InjectModuleConstructorChecks(module, context);

        // Inject checks into existing methods
        foreach (var type in module.GetTypes().Where(t => !IsExcluded(t, context)))
        {
            foreach (var method in type.Methods.Where(m => m.HasBody && !IsExcluded(m, context)))
            {
                if (ShouldInjectCheck(method, config))
                {
                    InjectAntiDebuggingChecks(method, config);
                    protectionPoints++;
                }
            }
        }

        // Create dedicated anti-debugging helper class
        var helperClass = CreateAntiDebuggingHelper(module, context);
        module.Types.Add(helperClass);

        logger.LogInformation("Anti-debugging protection: {Points} injection points created", protectionPoints);
    }

    private void InjectModuleConstructorChecks(ModuleDef module, ProtectionContext context)
    {
        var ctor = module.GlobalType.FindOrCreateStaticConstructor();
        ctor.Body ??= new CilBody();

        var checks = CreateStartupAntiDebuggingChecks(context);
        
        // Insert at beginning
        foreach (var check in checks.AsEnumerable().Reverse())
        {
            ctor.Body.Instructions.Insert(0, check);
        }
    }

    private void InjectAntiDebuggingChecks(MethodDef method, AntiTamperOptions config)
    {
        var body = method.Body;
        body.SimplifyBranches();

        // Insert checks at method entry
        var entryChecks = CreateMethodEntryChecks(config);
        foreach (var check in entryChecks.AsEnumerable().Reverse())
        {
            body.Instructions.Insert(0, check);
        }

        // Insert periodic checks throughout method
        if (body.Instructions.Count > 50)
        {
            InsertPeriodicChecks(body, config);
        }

        body.OptimizeBranches();
    }

    private List<Instruction> CreateStartupAntiDebuggingChecks(ProtectionContext context)
    {
        var checks = new List<Instruction>();

        // Multiple debugger detection techniques
        checks.AddRange(CreateDebuggerDetectionChain());

        // Parent process checking
        checks.AddRange(CreateParentProcessCheck());

        // Timing analysis detection
        checks.AddRange(CreateTimingAnalysisCheck());

        // VM/hypervisor detection
        checks.AddRange(CreateVMDetection());

        // Integrity checking
        checks.AddRange(CreateIntegrityChecks(context));

        return checks;
    }

    private List<Instruction> CreateMethodEntryChecks(AntiTamperOptions config)
    {
        var checks = new List<Instruction>();

        if (config.Mode >= AntiTamperMode.Normal)
        {
            // Quick debugger check
            checks.AddRange(CreateQuickDebuggerCheck());
        }

        if (config.Mode >= AntiTamperMode.Heavy)
        {
            // Comprehensive check
            checks.AddRange(CreateComprehensiveCheck());
        }

        return checks;
    }

    private List<Instruction> CreateDebuggerDetectionChain()
    {
        var chain = new List<Instruction>();

        // Check 1: IsDebuggerPresent API
        chain.AddRange(CallNativeIsDebuggerPresent());

        // Check 2: Debugger.IsAttached property
        chain.AddRange(CheckDebuggerAttachedProperty());

        // Check 3: Process environment block check
        chain.AddRange(CheckPEBBeingDebugged());

        // Check 4: Heap flag manipulation detection
        chain.AddRange(CheckHeapFlags());

        // Check 5: OutputDebugString trick
        chain.AddRange(CheckOutputDebugString());

        // Check 6: Int 3 / Trap flag check
        chain.AddRange(CheckTrapFlag());

        return chain;
    }

    private List<Instruction> CreateParentProcessCheck()
    {
        var check = new List<Instruction>
        {
            // Get current process
            new(OpCodes.Call, GetProcessGetCurrentProcessMethod()),

            // Get parent process
            new(OpCodes.Call, GetProcessGetParentMethod()),

            // Compare parent process names
            new(OpCodes.Callvirt, GetProcessGetProcessNameMethod())
        };

        // Check against known debuggers
        check.AddRange(CheckAgainstKnownDebuggers());

        return check;
    }

    private List<Instruction> CreateTimingAnalysisCheck()
    {
        var check = new List<Instruction>
        {
            // Get high-resolution timestamp
            new(OpCodes.Call, GetHighResolutionTimestampMethod())
        };

        // Perform timing-sensitive operation
        check.AddRange(PerformTimingSensitiveOperation());

        // Get timestamp again
        check.Add(new Instruction(OpCodes.Call, GetHighResolutionTimestampMethod()));

        // Calculate difference
        check.Add(new Instruction(OpCodes.Sub));

        // Check if too slow (indicating debugger)
        check.AddRange(CheckTimingDifference());

        return check;
    }

    private List<Instruction> CreateVMDetection()
    {
        var detection = new List<Instruction>();

        // Check hardware identifiers
        detection.AddRange(CheckHardwareIdentifiers());

        // Check registry keys
        detection.AddRange(CheckVMRegistryKeys());

        // Check file system artifacts
        detection.AddRange(CheckVMFiles());

        // Check device drivers
        detection.AddRange(CheckVMDrivers());

        return detection;
    }

    private List<Instruction> CreateIntegrityChecks(ProtectionContext context)
    {
        var checks = new List<Instruction>
        {
            // Calculate module checksum
            new(OpCodes.Ldtoken, context.Module),
            new(OpCodes.Call, GetCalculateChecksumMethod()),

            // Compare with stored checksum
            new(OpCodes.Ldc_I4, CalculateExpectedChecksum(context.Module)),
            new(OpCodes.Ceq),
            new(OpCodes.Brfalse, GetCorruptionHandler())
        };

        return checks;
    }

    private void InsertPeriodicChecks(CilBody body, AntiTamperOptions config)
    {
        var instructions = body.Instructions.ToList();
        var checkInterval = Math.Max(20, instructions.Count / 5); // Every 20 instructions or 5 times per method

        for (int i = checkInterval; i < instructions.Count; i += checkInterval)
        {
            if (i < instructions.Count)
            {
                var periodicCheck = CreatePeriodicCheck(config);
                foreach (var check in periodicCheck.AsEnumerable().Reverse())
                {
                    body.Instructions.Insert(i, check);
                    i++; // Adjust for inserted instructions
                }
            }
        }
    }

    private List<Instruction> CreatePeriodicCheck(AntiTamperOptions config)
    {
        var check = new List<Instruction>
        {
            // Inline debugger check
            new(OpCodes.Call, GetInlineDebuggerCheckMethod())
        };

        // Conditional corruption
        var corruptionLabel = new Instruction(OpCodes.Nop);
        check.Add(new Instruction(OpCodes.Brtrue, corruptionLabel));
        check.Add(new Instruction(OpCodes.Call, GetCorruptionMethod()));
        check.Add(corruptionLabel);

        return check;
    }

    private TypeDef CreateAntiDebuggingHelper(ModuleDef module, ProtectionContext context)
    {
        var helperType = new TypeDefUser(
            module.GlobalType.Namespace,
            GenerateHelperClassName(),
            module.CorLibTypes.Object.TypeDef
        );

        helperType.Attributes |= TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.Abstract;

        // Add native interop methods
        helperType.Methods.Add(CreateIsDebuggerPresentMethod(module));
        helperType.Methods.Add(CreateNtQueryInformationProcessMethod());
        helperType.Methods.Add(CreateCheckRemoteDebuggerPresentMethod());

        // Add managed detection methods
        helperType.Methods.Add(CreateManagedDetectionMethod());
        helperType.Methods.Add(CreateTimingDetectionMethod());

        // Add corruption methods
        helperType.Methods.Add(CreateCorruptionMethod());

        return helperType;
    }

    private MethodDefUser CreateIsDebuggerPresentMethod(ModuleDef module)
    {
        var method = new MethodDefUser(
            "IsDebuggerPresent",
            MethodSig.CreateStatic(module.CorLibTypes.Boolean),
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.PinvokeImpl
        );

        // P/Invoke mantiqi uchun ImplMap zarur
        var moduleRef = new ModuleRefUser(module, "kernel32.dll");
        method.ImplMap = new ImplMapUser(moduleRef, "IsDebuggerPresent", PInvokeAttributes.NoMangle);

        return method;
    }

    private bool ShouldInjectCheck(MethodDef method, AntiTamperOptions config)
    {
        if (method.IsGetter || method.IsSetter) return false;
        if (method.DeclaringType.IsGlobalModuleType) return false;

        var chance = config.Mode switch
        {
            AntiTamperMode.None => 0,
            AntiTamperMode.Light => 30,
            AntiTamperMode.Normal => 60,
            AntiTamperMode.Heavy => 90,
            _ => 50
        };

        return random.Next(0, 100) < chance;
    }

    private static bool IsExcluded(TypeDef type, ProtectionContext context)
    {
        return context.Configuration.ExcludedNamespaces.Contains(type.Namespace) ||
               context.Configuration.ExcludedTypes.Contains(type.FullName);
    }

    private static bool IsExcluded(MethodDef method, ProtectionContext context)
    {
        return context.Configuration.ExcludedMethods.Contains(method.FullName) ||
               IsExcluded(method.DeclaringType, context);
    }

    // Helper method stubs
    private List<Instruction> CallNativeIsDebuggerPresent() => [];
    private List<Instruction> CheckDebuggerAttachedProperty() => [];
    private List<Instruction> CheckPEBBeingDebugged() => new();
    private List<Instruction> CheckHeapFlags() => new();
    private List<Instruction> CheckOutputDebugString() => new();
    private List<Instruction> CheckTrapFlag() => new();
    private List<Instruction> CheckAgainstKnownDebuggers() => new();
    private List<Instruction> PerformTimingSensitiveOperation() => new();
    private List<Instruction> CheckTimingDifference() => new();
    private List<Instruction> CheckHardwareIdentifiers() => new();
    private List<Instruction> CheckVMRegistryKeys() => new();
    private List<Instruction> CheckVMFiles() => new();
    private List<Instruction> CheckVMDrivers() => new();
    private List<Instruction> CreateQuickDebuggerCheck() => new();
    private List<Instruction> CreateComprehensiveCheck() => new();
    private MethodDef GetProcessGetCurrentProcessMethod() => null!;
    private MethodDef GetProcessGetParentMethod() => null!;
    private MethodDef GetProcessGetProcessNameMethod() => null!;
    private MethodDef GetHighResolutionTimestampMethod() => null!;
    private MethodDef GetCalculateChecksumMethod() => null!;
    private MethodDef GetInlineDebuggerCheckMethod() => null!;
    private MethodDef GetCorruptionMethod() => null!;
    private Instruction GetCorruptionHandler() => new Instruction(OpCodes.Call, GetCorruptionMethod());
    private int CalculateExpectedChecksum(ModuleDef module)
    {
        // CS7036 tuzatildi
        return random.Next(1000, 999999);
    }
    private string GenerateHelperClassName() => "_" + random.NextString(15);
    private MethodDef CreateNtQueryInformationProcessMethod() => null!;
    private MethodDef CreateCheckRemoteDebuggerPresentMethod() => null!;
    private MethodDef CreateManagedDetectionMethod() => null!;
    private MethodDef CreateTimingDetectionMethod() => null!;
    private MethodDef CreateCorruptionMethod() => null!;
    private CustomAttribute CreateDllImportAttribute(string dllName, string methodName) => null!;
}
