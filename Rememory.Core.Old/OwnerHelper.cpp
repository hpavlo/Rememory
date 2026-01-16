#include "pch.h"
#include "OwnerHelper.h"
#include <tlhelp32.h>
#include <shlobj.h>
#include <shellapi.h>

#pragma region External functions

void GetOwnerIcon(const WCHAR* ownerPath, LONG* iconLength, BYTE** iconPixels)
{
    return OwnerHelper::LoadOwnerIcon(ownerPath, iconLength, iconPixels);
}

void FreeOwnerIcon(BYTE** iconPixels)
{
    if (*iconPixels)
    {
        free(*iconPixels);
        *iconPixels = nullptr;
    }
}

#pragma endregion

bool OwnerHelper::GetOwnerPath(const HWND owner, WCHAR* ownerPath)
{
    if (!owner)
    {
        return false;
    }

    DWORD processId = 0;
    GetWindowThreadProcessId(owner, &processId);

    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, processId);
    if (hSnapshot != INVALID_HANDLE_VALUE)
    {
        MODULEENTRY32 me32;
        me32.dwSize = sizeof(MODULEENTRY32);

        if (Module32First(hSnapshot, &me32))
        {
            do {
                if (me32.th32ProcessID == processId)
                {
                    wcscpy_s(ownerPath, MAX_PATH, me32.szExePath);
                    return true;
                }
            } while (Module32Next(hSnapshot, &me32));
        }
        CloseHandle(hSnapshot);
    }
    DWORD error = GetLastError();
    if (GetLastError() == ERROR_BAD_LENGTH)
    {
        return GetOwnerPath(owner, ownerPath);
    }
    ownerPath[0] = '\0';
    return false;
}

void OwnerHelper::LoadOwnerIcon(const WCHAR* ownerPath, LONG* iconLength, BYTE** iconPixels)
{
    HICON hIcon = ExtractIcon(NULL, ownerPath, 0);

    if (!hIcon)
    {
        *iconLength = 0;
        *iconPixels = nullptr;
        return;
    }

    ICONINFO iconInfo;
    BITMAP bmp;
    GetIconInfo(hIcon, &iconInfo);
    GetObject(iconInfo.hbmColor, sizeof(BITMAP), &bmp);

    HDC hdc = GetDC(NULL);

    BITMAPINFO bmi = {};
    bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    bmi.bmiHeader.biWidth = bmp.bmWidth;
    bmi.bmiHeader.biHeight = -bmp.bmHeight; // Negative to indicate a top-down DIB
    bmi.bmiHeader.biPlanes = 1;
    bmi.bmiHeader.biBitCount = 32; // Assuming 32 bits per pixel (BGRA)
    bmi.bmiHeader.biCompression = BI_RGB;

    BYTE* pixels = static_cast<BYTE*>(malloc(bmp.bmWidthBytes * bmp.bmHeight));
    GetDIBits(hdc, iconInfo.hbmColor, 0, bmp.bmHeight, pixels, &bmi, DIB_RGB_COLORS);

    ReleaseDC(NULL, hdc);

    *iconLength = bmp.bmWidthBytes * bmp.bmHeight;
    *iconPixels = hIcon ? pixels : nullptr;
}
