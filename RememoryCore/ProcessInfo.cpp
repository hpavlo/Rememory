#include "pch.h"
#include "ProcessInfo.h"
#include "ProcessInfo.g.cpp"
#pragma comment(lib, "shell32.lib")

namespace winrt::RememoryCore::implementation
{
    winrt::hstring ProcessInfo::GetProcessPath(UINT_PTR windowHandle) {
        HWND hWnd = reinterpret_cast<HWND>(windowHandle);

        if (!hWnd)
        {
            return {};
        }

        DWORD pid = 0;
        GetWindowThreadProcessId(hWnd, &pid);

        winrt::handle process(OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, pid));
        if (!process)
        {
            return {};
        }

        WCHAR path[MAX_PATH];
        DWORD size = MAX_PATH;
        if (QueryFullProcessImageNameW(process.get(), 0, path, &size)) {
            return winrt::hstring{ path, size };
        }

        return {};
    }

    winrt::Windows::Storage::Streams::IBuffer ProcessInfo::GetProcessIcon(winrt::hstring const& processPath)
    {
        HICON hIcon = ExtractIconW(nullptr, processPath.c_str(), 0);
        if (!hIcon)
        {
            return nullptr;
        }

        ICONINFO iconInfo;
        if (!GetIconInfo(hIcon, &iconInfo))
        {
            DestroyIcon(hIcon);
            return nullptr;
        }

        BITMAP bmp;
        if (!GetObject(iconInfo.hbmColor, sizeof(BITMAP), &bmp)) {
            DeleteObject(iconInfo.hbmColor);
            DeleteObject(iconInfo.hbmMask);
            DestroyIcon(hIcon);
            return nullptr;
        }

        // 32 bits per pixel (4 bytes: B, G, R, A)
        uint32_t byteSize = bmp.bmWidth * bmp.bmHeight * 4;
        winrt::Windows::Storage::Streams::Buffer buffer{ byteSize };
        buffer.Length(byteSize);

        BITMAPINFO bmi = {};
        bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
        bmi.bmiHeader.biWidth = bmp.bmWidth;
        bmi.bmiHeader.biHeight = -bmp.bmHeight; // Top-down
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32;
        bmi.bmiHeader.biCompression = BI_RGB;

        HDC hdc = GetDC(nullptr);
        GetDIBits(hdc, iconInfo.hbmColor, 0, bmp.bmHeight, buffer.data(), &bmi, DIB_RGB_COLORS);
        ReleaseDC(nullptr, hdc);

        // Manual cleanup before returning the result
        DeleteObject(iconInfo.hbmColor);
        DeleteObject(iconInfo.hbmMask);
        DestroyIcon(hIcon);

        return buffer;
    }
}
