using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpGuard.Core.Abstractions;
using SharpGuard.Core.Configuration;
using SharpGuard.Core.Services;
using ILogger = SharpGuard.Core.Services.ILogger;

namespace SharpGuard.Core.Strategies;

/// <summary>
/// Advanced string encryption with dynamic decryption and anti-dumping techniques
/// Implements Chain of Responsibility and Proxy patterns
/// </summary>
public class StringEncryptionStrategy(
    IRandomGenerator random, 
    ILogger logger
) : IProtectionStrategy
{
    public string Id => "stringenc";
    public string Name => "Advanced String Encryption";
    public string Description => "Encrypts strings with dynamic decryption and anti-memory dumping protection";
    public int Priority => 900;
    public ImmutableArray<string> Dependencies => [];
    public ImmutableArray<string> ConflictsWith => [];

    private readonly Dictionary<string, EncryptedString> _encryptedStrings = [];

    public bool CanApply(ModuleDef module)
    {
        return module.Types.Any(t => t.Methods.Any(m => m.HasBody && HasStrings(m)));
    }

    public void Apply(ModuleDef module, ProtectionContext context)
    {
        var config = context.Configuration.Encryption;
        if (!config.Enabled || !config.EncryptStrings) return;

        var processedStrings = 0;
        
        // First pass: collect and encrypt all strings
        CollectAndEncryptStrings(module, context);
        
        // Second pass: replace strings with decryption calls
        ReplaceStringsWithDecryptionCalls(module, context, config);
        
        // Third pass: inject decryption runtime
        InjectDecryptionRuntime(module, context, config);
        
        logger.LogInformation("String encryption: {Count} strings encrypted", _encryptedStrings.Count);
    }

    private void CollectAndEncryptStrings(ModuleDef module, ProtectionContext context)
    {
        foreach (var type in module.GetTypes())
        {
            foreach (var method in type.Methods.Where(m => m.HasBody))
            {
                var instructions = method.Body.Instructions;
                for (int i = 0; i < instructions.Count; i++)
                {
                    var instruction = instructions[i];
                    if (instruction.OpCode == OpCodes.Ldstr && instruction.Operand is string strValue)
                    {
                        if (!string.IsNullOrEmpty(strValue) && !IsExcludedString(strValue, context))
                        {
                            var encrypted = EncryptString(strValue, context);
                            _encryptedStrings[strValue] = encrypted;
                        }
                    }
                }
            }
        }
    }

    private void ReplaceStringsWithDecryptionCalls(ModuleDef module, ProtectionContext context, EncryptionOptions config)
    {
        foreach (var type in module.GetTypes())
        {
            foreach (var method in type.Methods.Where(m => m.HasBody))
            {
                ReplaceMethodStrings(method, config);
            }
        }
    }

    private void ReplaceMethodStrings(MethodDef method, EncryptionOptions config)
    {
        var body = method.Body;
        var instructions = body.Instructions.ToList();
        
        for (int i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];
            if (instruction.OpCode == OpCodes.Ldstr && instruction.Operand is string strValue)
            {
                if (_encryptedStrings.TryGetValue(strValue, out var encrypted))
                {
                    // Replace with decryption call chain
                    var replacement = CreateDecryptionCallChain(encrypted, config);
                    
                    // Remove original instruction
                    body.Instructions.RemoveAt(i);
                    
                    // Insert decryption chain
                    for (int j = replacement.Count - 1; j >= 0; j--)
                    {
                        body.Instructions.Insert(i, replacement[j]);
                    }
                    
                    // Adjust index for next iteration
                    i += replacement.Count - 1;
                }
            }
        }
    }

    private List<Instruction> CreateDecryptionCallChain(EncryptedString encrypted, EncryptionOptions config)
    {
        var chain = new List<Instruction>();
        
        if (config.DynamicDecryption)
        {
            // Multi-stage dynamic decryption
            chain.AddRange(CreateDynamicDecryptionChain(encrypted));
        }
        else
        {
            // Static decryption
            chain.AddRange(CreateStaticDecryptionChain(encrypted));
        }
        
        return chain;
    }

    private List<Instruction> CreateDynamicDecryptionChain(EncryptedString encrypted)
    {
        var chain = new List<Instruction>();
        
        // Stage 1: Load encrypted data
        chain.Add(new Instruction(OpCodes.Ldtoken, CreateByteArray(encrypted.EncryptedData)));
        
        // Stage 2: Load key (obfuscated)
        var obfuscatedKey = ObfuscateKey(encrypted.Key);
        chain.Add(new Instruction(OpCodes.Ldtoken, CreateByteArray(obfuscatedKey)));
        
        // Stage 3: Call dynamic decryptor
        chain.Add(new Instruction(OpCodes.Call, GetOrCreateDynamicDecryptor()));
        
        return chain;
    }

    private List<Instruction> CreateStaticDecryptionChain(EncryptedString encrypted)
    {
        var chain = new List<Instruction>();
        
        // Load encrypted bytes
        chain.Add(new Instruction(OpCodes.Ldtoken, CreateByteArray(encrypted.EncryptedData)));
        
        // Call static decryptor
        chain.Add(new Instruction(OpCodes.Call, GetOrCreateStaticDecryptor()));
        
        return chain;
    }

    private void InjectDecryptionRuntime(ModuleDef module, ProtectionContext context, EncryptionOptions config)
    {
        // Create hidden decryption helper class
        var helperClass = CreateDecryptionHelperClass(module, config);
        module.Types.Add(helperClass);
        
        // Inject anti-debugging checks in decryption routines
        if (context.Configuration.EnableAntiDebugging)
        {
            InjectAntiDebuggingChecks(helperClass);
        }
        
        // Inject anti-dumping protection
        if (context.Configuration.EnableAntiTampering)
        {
            InjectAntiDumpingProtection(helperClass);
        }
    }

    private TypeDefUser CreateDecryptionHelperClass(ModuleDef module, EncryptionOptions config)
    {
        var typeName = GenerateObfuscatedName();
        var helperType = new TypeDefUser(module.GlobalType.Namespace, typeName, module.CorLibTypes.Object.TypeDef);
        
        // Make it internal and compiler-generated
        helperType.Attributes |= TypeAttributes.NotPublic | TypeAttributes.Sealed;
        helperType.CustomAttributes.Add(CreateCompilerGeneratedAttribute(module));
        
        // Create decryption methods
        helperType.Methods.Add(CreateStaticDecryptorMethod(module, config));
        helperType.Methods.Add(CreateDynamicDecryptorMethod(module, config));
        
        return helperType;
    }

    private MethodDefUser CreateStaticDecryptorMethod(ModuleDef module, EncryptionOptions config)
    {
        var method = new MethodDefUser(
            GenerateObfuscatedMethodName(),
            MethodSig.CreateStatic(module.CorLibTypes.String, module.ImportAsTypeSig(typeof(System.RuntimeFieldHandle))),
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig
        );
        
        var body = new CilBody();
        method.Body = body;
        
        // Anti-analysis prologue
        InjectAntiAnalysisPrologue(body);
        
        // Decryption logic based on algorithm
        switch (config.Algorithm)
        {
            case EncryptionAlgorithm.AES:
                GenerateAESDecryptionLogic(body);
                break;
            case EncryptionAlgorithm.ChaCha20:
                GenerateChaCha20DecryptionLogic(body);
                break;
            default:
                GenerateCustomDecryptionLogic(body);
                break;
        }
        
        // Anti-dumping epilogue
        InjectAntiDumpingEpilogue(body);
        
        body.Instructions.Add(new Instruction(OpCodes.Ret));
        
        return method;
    }

    private MethodDefUser CreateDynamicDecryptorMethod(ModuleDef module, EncryptionOptions config)
    {
        var method = new MethodDefUser(
            GenerateObfuscatedMethodName(),
            MethodSig.CreateStatic(module.CorLibTypes.String,
                module.ImportAsTypeSig(typeof(RuntimeFieldHandle)), // encrypted data
                module.ImportAsTypeSig(typeof(RuntimeFieldHandle))), // key
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig
        );
        
        var body = new CilBody();
        method.Body = body;
        
        // Complex key derivation and validation
        GenerateKeyDerivationLogic(body);
        
        // Multi-layer decryption
        GenerateMultiLayerDecryption(body, config);
        
        body.Instructions.Add(new Instruction(OpCodes.Ret));
        
        return method;
    }

    private EncryptedString EncryptString(string value, ProtectionContext context)
    {
        var config = context.Configuration.Encryption;
        var key = GenerateEncryptionKey();
        
        byte[] encryptedData = config.Algorithm switch
        {
            EncryptionAlgorithm.AES => EncryptWithAES(value, key),
            EncryptionAlgorithm.ChaCha20 => EncryptWithChaCha20(value, key),
            _ => EncryptWithCustomAlgorithm(value, key)
        };
        
        return new EncryptedString(value, encryptedData, key, config.Algorithm);
    }

    private static byte[] EncryptWithAES(string value, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(key);
        aes.IV = new byte[16]; // Zero IV for deterministic encryption
        
        using var encryptor = aes.CreateEncryptor();
        var data = Encoding.UTF8.GetBytes(value);
        return encryptor.TransformFinalBlock(data, 0, data.Length);
    }

    private byte[] EncryptWithChaCha20(string value, byte[] key)
    {
        // Simplified ChaCha20 implementation
        var data = Encoding.UTF8.GetBytes(value);
        var nonce = new byte[12];
        var counter = 1u;
        
        // XOR with pseudo-random stream
        var keystream = GenerateChaCha20Keystream(key, nonce, counter, (uint)data.Length);
        var result = new byte[data.Length];
        
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ keystream[i]);
        }
        
        return result;
    }

    private static byte[] EncryptWithCustomAlgorithm(string value, byte[] key)
    {
        var data = Encoding.UTF8.GetBytes(value);
        var result = new byte[data.Length];
        
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ key[i % key.Length] ^ (i * 17));
        }
        
        return result;
    }

    private FieldDef CreateByteArray(byte[] data)
    {
        // In practice, this would create a static field with the byte array
        // For brevity, returning null here
        return null!;
    }

    private MethodDef GetOrCreateStaticDecryptor()
    {
        // Would return reference to static decryptor method
        return null!;
    }

    private MethodDef GetOrCreateDynamicDecryptor()
    {
        // Would return reference to dynamic decryptor method
        return null!;
    }

    private byte[] GenerateEncryptionKey()
    {
        return random.NextBytes(32);
    }

    private byte[] ObfuscateKey(byte[] key)
    {
        // Simple key obfuscation - in practice would be much more complex
        var obfuscated = new byte[key.Length];
        for (int i = 0; i < key.Length; i++)
        {
            obfuscated[i] = (byte)(key[i] ^ 0xAA);
        }
        return obfuscated;
    }

    private string GenerateObfuscatedName()
    {
        return "_" + random.NextString(16);
    }

    private string GenerateObfuscatedMethodName()
    {
        return "__" + random.NextString(12);
    }

    private static bool HasStrings(MethodDef method)
    {
        return method.Body?.Instructions.Any(i => i.OpCode == OpCodes.Ldstr) ?? false;
    }

    private bool IsExcludedString(string value, ProtectionContext context)
    {
        // Exclude very short strings, common framework strings, etc.
        return value.Length < 2 || 
               value.StartsWith("System.") || 
               value.StartsWith("Microsoft.") ||
               context.Configuration.ExcludedMethods.Contains(value);
    }

    // Stub methods - would contain actual implementation
    private void InjectAntiAnalysisPrologue(CilBody body) { }
    private void InjectAntiDumpingEpilogue(CilBody body) { }
    private void InjectAntiDebuggingChecks(TypeDef helperType) { }
    private void InjectAntiDumpingProtection(TypeDef helperType) { }
    private void GenerateAESDecryptionLogic(CilBody body) { }
    private void GenerateChaCha20DecryptionLogic(CilBody body) { }
    private void GenerateCustomDecryptionLogic(CilBody body) { }
    private void GenerateKeyDerivationLogic(CilBody body) { }
    private void GenerateMultiLayerDecryption(CilBody body, EncryptionOptions config) { }
    private byte[] GenerateChaCha20Keystream(byte[] key, byte[] nonce, uint counter, uint length) => new byte[length];
    private CustomAttribute CreateCompilerGeneratedAttribute(ModuleDef module) => null!;
}

public record EncryptedString(
    string OriginalValue,
    byte[] EncryptedData,
    byte[] Key,
    EncryptionAlgorithm Algorithm
);
