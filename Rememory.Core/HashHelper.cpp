#include "pch.h"
#include "HashHelper.h"
#include <wincrypt.h>
#include <shlobj.h>

//Use free() for returned value
BYTE* HashHelper::ComputeSHA256(void* data, DWORD dataLength)
{
    HCRYPTPROV hProv = NULL;
    HCRYPTHASH hHash = NULL;

    if (!CryptAcquireContext(&hProv, NULL, NULL, PROV_RSA_AES, CRYPT_VERIFYCONTEXT))
    {
        return nullptr;
    }

    if (!CryptCreateHash(hProv, CALG_SHA_256, 0, 0, &hHash))
    {
        CryptReleaseContext(hProv, 0);
        return nullptr;
    }

    if (!CryptHashData(hHash, static_cast<BYTE*>(data), dataLength, 0))
    {
        CryptDestroyHash(hHash);
        CryptReleaseContext(hProv, 0);
        return nullptr;
    }

    DWORD hashLen = 0;
    DWORD len = sizeof(DWORD);

    if (!CryptGetHashParam(hHash, HP_HASHSIZE, reinterpret_cast<BYTE*>(&hashLen), &len, 0))
    {
        CryptDestroyHash(hHash);
        CryptReleaseContext(hProv, 0);
        return nullptr;
    }

    BYTE* hash = static_cast<BYTE*>(malloc(32));

    if (!CryptGetHashParam(hHash, HP_HASHVAL, hash, &hashLen, 0))
    {
        CryptDestroyHash(hHash);
        CryptReleaseContext(hProv, 0);
        return nullptr;
    }

    CryptDestroyHash(hHash);
    CryptReleaseContext(hProv, 0);

    return hash;
}
