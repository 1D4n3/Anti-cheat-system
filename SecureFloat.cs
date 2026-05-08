using System;
using System.Security.Cryptography;

[Serializable]
public struct SecureFloat : IEquatable<SecureFloat>
{
    private const int NotInitialized = 0;

    private int _cryptoKey;
    private int _encryptedValue;
    private int _integrity;
    private int _initialized;

    public SecureFloat(float value)
    {
        _cryptoKey = GenerateKey();
        int bits = BitConverter.SingleToInt32Bits(value);
        _encryptedValue = bits ^ _cryptoKey;
        _integrity = ComputeIntegrity(bits, _cryptoKey);
        _initialized = 1;
    }

    public float Value
    {
        get
        {
            if (_initialized == NotInitialized)
                return 0f;

            return BitConverter.Int32BitsToSingle(_encryptedValue ^ _cryptoKey);
        }
    }

    public bool IsValid
    {
        get
        {
            if (_initialized == NotInitialized)
                return true;

            int bits = _encryptedValue ^ _cryptoKey;
            return _integrity == ComputeIntegrity(bits, _cryptoKey);
        }
    }

    public bool TryGetValue(out float value)
    {
        value = Value;
        return IsValid;
    }

    public void SetValue(float value)
    {
        if (_initialized == NotInitialized)
        {
            _cryptoKey = GenerateKey();
            _initialized = 1;
        }

        int bits = BitConverter.SingleToInt32Bits(value);
        _encryptedValue = bits ^ _cryptoKey;
        _integrity = ComputeIntegrity(bits, _cryptoKey);
    }

    public void Rekey()
    {
        if (_initialized == NotInitialized)
        {
            _cryptoKey = GenerateKey();
            _integrity = ComputeIntegrity(0, _cryptoKey);
            _initialized = 1;
            return;
        }

        int bits = _encryptedValue ^ _cryptoKey;
        _cryptoKey = GenerateKey();
        _encryptedValue = bits ^ _cryptoKey;
        _integrity = ComputeIntegrity(bits, _cryptoKey);
    }

    public static implicit operator float(SecureFloat secure) => secure.Value;
    public static implicit operator SecureFloat(float value) => new SecureFloat(value);

    public bool Equals(SecureFloat other) => Value.Equals(other.Value);
    public override bool Equals(object obj) => obj is SecureFloat other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();

    private static int GenerateKey()
    {
        int key;
        var bytes = new byte[4];
        do
        {
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            key = BitConverter.ToInt32(bytes, 0);
        } while (key == 0);

        return key;
    }

    private static int ComputeIntegrity(int bits, int key)
    {
        unchecked
        {
            uint x = (uint)bits;
            uint k = (uint)key;
            uint h = 0x85EBCA6Bu;
            h ^= x + 0xC2B2AE35u + (h << 6) + (h >> 2);
            h ^= k + 0x27D4EB2Fu + (h << 6) + (h >> 2);
            return (int)h;
        }
    }
}