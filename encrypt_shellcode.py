from Crypto.Cipher import AES
from Crypto.Random import get_random_bytes
import base64

# Function to pad the data to be a multiple of 16 bytes


def pad(data):
    pad_len = 16 - (len(data) % 16)
    if pad_len == 0:
        pad_len = 16
    return data + bytes([pad_len] * pad_len)


# Read shellcode from file
with open('shellcode.bin', 'rb') as f:
    shellcode = f.read()

# Create AES key and IV
key = get_random_bytes(32)
iv = get_random_bytes(16)
cipher = AES.new(key, AES.MODE_CBC, iv)

# Encrypt shellcode (padding)
encrypted_shellcode = cipher.encrypt(pad(shellcode))

with open('encrypted_shellcode.bin', 'wb') as f:
    f.write(iv + encrypted_shellcode)

with open('key.txt', 'wb') as f:
    f.write(base64.b64encode(key))

print("Encrypt shellcode file in 'encrypted_shellcode.bin'")
print("Key in 'key.txt'")
