#include "pch.h"
#include <numeric>
#include <fstream>
#include <ShlObj.h>
#include <gdiplus.h>
#include "FormatManager.h"
#include "FormatManager.g.cpp"
#pragma comment(lib, "gdiplus.lib")
#pragma comment(lib, "gdi32.lib")

#define CF_PREFERREDDROPEFFECT RegisterClipboardFormat(CFSTR_PREFERREDDROPEFFECT)

namespace winrt::RememoryCore::implementation
{
    winrt::hstring FormatManager::FormatToName(ClipboardFormat format)
    {
        return formatNames.find(format)->second;
    }

    ClipboardFormat FormatManager::FormatFromName(winrt::hstring formatName)
    {
        for (auto const& [format, name] : formatNames)
        {
            if (name == formatName)
            {
                return format;
            }
        }

        throw winrt::hresult_invalid_argument(L"Unknown clipboard format name provided: " + formatName);
    }

    winrt::hstring FormatManager::GenerateFileName(ClipboardFormat format)
    {
        static const auto& fileNameFormat = L"{:%Y%m%d_%H%M%S}{:03d}.{}";
        static const std::unordered_map<ClipboardFormat, std::wstring_view> extensions
        {
            { ClipboardFormat::Rtf, L"rtf" },
            { ClipboardFormat::Html, L"html" },
            { ClipboardFormat::Png, L"png" },
            { ClipboardFormat::Bitmap, L"bmp" }
        };

        auto it = extensions.find(format);
        if (it == extensions.end())
        {
            return {};
        }

        auto local_now = std::chrono::zoned_time{ std::chrono::current_zone(), std::chrono::system_clock::now() }.get_local_time();
        auto seconds   = std::chrono::floor<std::chrono::seconds>(local_now);
        auto ms        = std::chrono::duration_cast<std::chrono::milliseconds>(local_now.time_since_epoch()) % 1000;

        auto fileName = std::format(fileNameFormat, seconds, static_cast<int>(ms.count()), it->second);

        return winrt::hstring{ fileName };
    }

    winrt::hstring FormatManager::GetFormatFolderName(ClipboardFormat format)
    {
        static const std::unordered_map<ClipboardFormat, winrt::hstring> folderNames
        {
            { ClipboardFormat::Rtf, RtfFolderName()},
            { ClipboardFormat::Html, HtmlFolderName() },
            { ClipboardFormat::Png, PngFolderName() },
            { ClipboardFormat::Bitmap, BitmapFolderName() }
        };

        auto it = folderNames.find(format);
        if (it == folderNames.end())
        {
            return {};
        }

        return it->second;
    }


    bool FormatManager::GetGeneralDataCopy(HANDLE hData, size_t maxDataSize, ClipboardData* clipboardData)
    {
        size_t dataSize = GlobalSize(hData);
        if (dataSize == 0 || dataSize > maxDataSize)
        {
            return false;
        }

        LPVOID pSource = GlobalLock(hData);
        if (!pSource)
        {
            return false;
        }

        LPVOID pCopy = malloc(dataSize);
        if (!pCopy)
        {
            GlobalUnlock(hData);
            return false;
        }

        memcpy(pCopy, pSource, dataSize);
        GlobalUnlock(hData);

        clipboardData->data = pCopy;
        clipboardData->size = dataSize;

        return true;
    }

    bool FormatManager::GetFilesDataCopy(HANDLE hData, size_t maxDataSize, ClipboardData* clipboardData)
    {
        HANDLE hDropEffect = GetClipboardData(CF_PREFERREDDROPEFFECT);
        LPDWORD dropEffect = (LPDWORD)GlobalLock(hDropEffect);

        if (dropEffect)
        {
            if ((*dropEffect & DROPEFFECT_COPY) != DROPEFFECT_COPY)
            {
                GlobalUnlock(hDropEffect);
                return false;
            }

            GlobalUnlock(hDropEffect);
        }

        HDROP hDrop = (HDROP)GlobalLock(hData);
        if (!hDrop)
        {
            return false;
        }

        UINT fileCount = DragQueryFile(hDrop, 0xFFFFFFFF, nullptr, 0);
        std::vector<std::wstring> paths;

        for (UINT i = 0; i < fileCount; ++i)
        {
            UINT requiredLength = DragQueryFile(hDrop, i, nullptr, 0);
            std::wstring filePath;
            filePath.resize(requiredLength);

            DragQueryFile(hDrop, i, filePath.data(), requiredLength + 1);
            paths.push_back(filePath);
        }

        size_t totalLength = std::accumulate(paths.begin(), paths.end(), 0ULL,
            [](size_t sum, const std::wstring& s) { return sum + s.length(); });

        // Add space for separators
        totalLength += ((paths.size() > 0) ? paths.size() - 1 : 0);

        if (totalLength > maxDataSize)
        {
            GlobalUnlock(hData);
            return false;
        }

        std::wstring joined;
        joined.reserve(totalLength);
        for (size_t i = 0; i < paths.size(); ++i)
        {
            joined.append(paths[i]);
            if (i < paths.size() - 1)
            {
                joined.append(FilePathsSeparator());
            }
        }

        size_t dataSize = joined.size() * sizeof(wchar_t);

        LPVOID pCopy = malloc(dataSize);
        if (!pCopy)
        {
            GlobalUnlock(hData);
            return false;
        }

        memcpy(pCopy, joined.c_str(), dataSize);
        GlobalUnlock(hData);

        clipboardData->data = pCopy;
        clipboardData->size = dataSize;

        return true;
    }

    bool FormatManager::GetBitmapDataCopy(HANDLE hData, size_t maxDataSize, ClipboardData* clipboardData)
    {
        HBITMAP hBitmap = (HBITMAP)hData;
        if (!hBitmap)
        {
            return false;
        }

        BITMAP bitmapInfo = {};
        if (GetObject(hBitmap, sizeof(BITMAP), &bitmapInfo) == 0)
        {
            return false;
        }

        BITMAPINFO bmi = {};
        bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32;
        bmi.bmiHeader.biCompression = BI_RGB;
        bmi.bmiHeader.biWidth = bitmapInfo.bmWidth;
        bmi.bmiHeader.biHeight = -abs(bitmapInfo.bmHeight);

        HDC hdcScreen = GetDC(NULL);
        if (!hdcScreen)
        {
            return false;
        }

        HDC hdcMem = CreateCompatibleDC(hdcScreen);
        if (!hdcMem)
        {
            ReleaseDC(NULL, hdcScreen);
            return false;
        }

        GetDIBits(hdcMem, hBitmap, 0, 0, NULL, &bmi, DIB_RGB_COLORS);

        unsigned int width = bmi.bmiHeader.biWidth;
        unsigned int height = abs(bmi.bmiHeader.biHeight);

        if (bmi.bmiHeader.biSizeImage == 0)
        {
            DWORD dwBytesPerRow = ((bmi.bmiHeader.biWidth * bmi.bmiHeader.biBitCount + 31) / 32) * 4;
            bmi.bmiHeader.biSizeImage = dwBytesPerRow * height;
        }

        if (bmi.bmiHeader.biSizeImage > maxDataSize)
        {
            DeleteDC(hdcMem);
            ReleaseDC(NULL, hdcScreen);
            return false;
        }

        LPVOID pixelData = malloc(bmi.bmiHeader.biSizeImage);
        if (!pixelData)
        {
            DeleteDC(hdcMem);
            ReleaseDC(NULL, hdcScreen);
            return false;
        }

        if (GetDIBits(hdcMem, hBitmap, 0, height, pixelData, &bmi, DIB_RGB_COLORS) == 0)
        {
            DeleteDC(hdcMem);
            ReleaseDC(NULL, hdcScreen);
            free(pixelData);
            return false;
        }

        DeleteDC(hdcMem);
        ReleaseDC(NULL, hdcScreen);

        LPVOID header = malloc(bmi.bmiHeader.biSize);
        if (!header)
        {
            free(pixelData);
            return false;
        }

        memcpy(header, &bmi.bmiHeader, bmi.bmiHeader.biSize);

        clipboardData->data = pixelData;
        clipboardData->size = bmi.bmiHeader.biSizeImage;
        clipboardData->header = header;

        return true;
    }


    winrt::Windows::Foundation::IAsyncOperation<winrt::hstring> FormatManager::SaveGeneralDataToFile(std::filesystem::path rootHistoryFolder, ClipboardFormat format, const ClipboardData* clipboardData)
    {
        if (!clipboardData->data || clipboardData->size == 0)
        {
            co_return {};
        }

        auto formatFolderName = GetFormatFolderName(format).c_str();
        auto fileName = GenerateFileName(format).c_str();
        std::filesystem::path fullPath{ rootHistoryFolder / formatFolderName / fileName };

        // Ensure folder exists
        std::filesystem::create_directories(fullPath.parent_path());

        std::ofstream file{ fullPath, std::ios::binary | std::ios::trunc };
        if (file.is_open())
        {
            file.write(reinterpret_cast<const char*>(clipboardData->data), clipboardData->size);
            file.close();

            co_return winrt::hstring{ fullPath.wstring() };
        }

        co_return {};
    }

    winrt::Windows::Foundation::IAsyncOperation<winrt::hstring> FormatManager::SaveBitmapToFile(std::filesystem::path rootHistoryFolder, ClipboardFormat format, const ClipboardData* clipboardData)
    {
        if (!clipboardData->data || clipboardData->size == 0)
        {
            co_return {};
        }

        auto formatFolderName = GetFormatFolderName(format).c_str();
        auto fileName = GenerateFileName(format).c_str();
        std::filesystem::path fullPath{ rootHistoryFolder / formatFolderName / fileName };

        // Ensure folder exists
        std::filesystem::create_directories(fullPath.parent_path());

        try
        {
            auto folder = co_await winrt::Windows::Storage::StorageFolder::GetFolderFromPathAsync(winrt::hstring(fullPath.parent_path().wstring()));
            auto file = co_await folder.CreateFileAsync(fileName, winrt::Windows::Storage::CreationCollisionOption::ReplaceExisting);
            auto stream = co_await file.OpenAsync(winrt::Windows::Storage::FileAccessMode::ReadWrite);
            auto encoder = co_await winrt::Windows::Graphics::Imaging::BitmapEncoder::CreateAsync(winrt::Windows::Graphics::Imaging::BitmapEncoder::PngEncoderId(), stream);

            auto* pBitmapHeader = reinterpret_cast<BITMAPINFOHEADER*>(clipboardData->header);
            if (pBitmapHeader->biWidth <= 0 || pBitmapHeader->biHeight == 0)
            {
                co_return {};
            }

            winrt::array_view<const uint8_t> pixels(reinterpret_cast<const uint8_t*>(clipboardData->data), clipboardData->size);

            encoder.SetPixelData(
                winrt::Windows::Graphics::Imaging::BitmapPixelFormat::Bgra8,
                winrt::Windows::Graphics::Imaging::BitmapAlphaMode::Ignore,
                pBitmapHeader->biWidth,
                abs(pBitmapHeader->biHeight),
                96.0, 96.0,
                pixels
            );

            co_await encoder.FlushAsync();
            stream.Close();

            co_return file.Path();
        }
        catch (const hresult_error& err) {}

        co_return {};
    }


    bool FormatManager::LoadGeneralDataToClipboard(UINT formatId, const winrt::hstring& data)
    {
        if (data.empty())
        {
            return false;
        }

        std::filesystem::path filePath{ data.c_str()};
        if (!std::filesystem::exists(filePath) || !std::filesystem::is_regular_file(filePath))
        {
            return false;
        }

        uintmax_t fileSize = std::filesystem::file_size(filePath);
        if (fileSize == 0 || fileSize > static_cast<uintmax_t>((SIZE_T)-1))
        {
            return false;
        }

        HGLOBAL hGlobal = GlobalAlloc(GMEM_MOVEABLE, static_cast<SIZE_T>(fileSize));
        if (!hGlobal)
        {
            return false;
        }

        void* pData = GlobalLock(hGlobal);
        if (!pData)
        {
            GlobalFree(hGlobal);
            return false;
        }

        std::ifstream file{ filePath, std::ios::binary };
        if (!file.read(static_cast<char*>(pData), fileSize))
        {
            GlobalUnlock(hGlobal);
            GlobalFree(hGlobal);
            return false;
        }

        GlobalUnlock(hGlobal);

        if (!SetClipboardData(formatId, hGlobal))
        {
            GlobalFree(hGlobal);
            return false;
        }

        return true;
    }

    bool FormatManager::LoadUnicodeToClipboard(UINT formatId, const winrt::hstring& data)
    {
        std::wstring_view textView{ data };

        if (textView.empty())
        {
            return false;
        }

        // We need (length + 1) to include the null terminator (L'\0')
        size_t charCountWithNull = textView.size() + 1;
        size_t sizeInBytes = charCountWithNull * sizeof(wchar_t);

        HGLOBAL hGlobal = GlobalAlloc(GMEM_MOVEABLE, sizeInBytes);
        if (!hGlobal)
        {
            return false;
        }

        void* pData = GlobalLock(hGlobal);
        if (!pData)
        {
            GlobalFree(hGlobal);
            return false;
        }

        memcpy(pData, textView.data(), sizeInBytes);

        GlobalUnlock(hGlobal);

        if (!SetClipboardData(formatId, hGlobal))
        {
            GlobalFree(hGlobal);
            return false;
        }

        return true;
    }

    bool FormatManager::LoadFilesToClipboard(UINT formatId, const winrt::hstring& data)
    {
        std::wstring_view dataView{ data };

        if (dataView.empty())
        {
            return false;
        }

        std::vector<std::wstring> paths;
        std::wstring_view separator{ FilePathsSeparator() };

        size_t start = 0;
        size_t end = dataView.find(separator);
        while (end != std::wstring_view::npos)
        {
            paths.emplace_back(dataView.substr(start, end - start));
            start = end + separator.length();
            end = dataView.find(separator, start);
        }
        paths.emplace_back(dataView.substr(start));

        size_t pathsTotalChars = 0;
        for (const auto& path : paths)
        {
            pathsTotalChars += (path.length() + 1); // +1 for null terminator
        }
        pathsTotalChars += 1; // Final double-null terminator

        size_t totalSize = sizeof(DROPFILES) + (pathsTotalChars * sizeof(wchar_t));

        HGLOBAL hGlobal = GlobalAlloc(GMEM_MOVEABLE | GMEM_ZEROINIT, totalSize);
        if (!hGlobal)
        {
            return false;
        }

        DROPFILES* pDrop = static_cast<DROPFILES*>(GlobalLock(hGlobal));
        if (!pDrop)
        {
            GlobalFree(hGlobal);
            return false;
        }

        pDrop->pFiles = sizeof(DROPFILES); // Offset to where files start
        pDrop->fWide = TRUE;

        wchar_t* pPathBuffer = reinterpret_cast<wchar_t*>(reinterpret_cast<BYTE*>(pDrop) + sizeof(DROPFILES));
        size_t currentOffset = 0;
        for (const auto& path : paths)
        {
            wcscpy_s(pPathBuffer + currentOffset, path.length() + 1, path.c_str());
            currentOffset += (path.length() + 1);
        }

        GlobalUnlock(hGlobal);

        if (!SetClipboardData(formatId, hGlobal))
        {
            GlobalFree(hGlobal);
            return false;
        }

        DWORD dropEffect = DROPEFFECT_COPY;

        HGLOBAL hGlobalEffect = GlobalAlloc(GMEM_MOVEABLE, sizeof(DWORD));
        if (hGlobalEffect)
        {
            DWORD* pEffect = static_cast<DWORD*>(GlobalLock(hGlobalEffect));
            if (pEffect)
            {
                *pEffect = dropEffect;
                GlobalUnlock(hGlobalEffect);

                if (!SetClipboardData(CF_PREFERREDDROPEFFECT, hGlobalEffect))
                {
                    GlobalFree(hGlobalEffect);
                }
            }
            else
            {
                GlobalFree(hGlobalEffect);
            }
        }

        return true;
    }

    bool FormatManager::LoadImageToClipboard(UINT formatId, const winrt::hstring& data)
    {
        bool result = LoadGeneralDataToClipboard(formatId, data);
        LoadBitmapToClipboard(CF_BITMAP, data);

        return result;
    }

    bool FormatManager::LoadBitmapToClipboard(UINT formatId, const winrt::hstring& data)
    {
        if (data.empty())
        {
            return false;
        }

        Gdiplus::Bitmap bitmap(data.c_str());
        if (bitmap.GetLastStatus() != Gdiplus::Ok)
        {
            return false;
        }

        UINT width = bitmap.GetWidth();
        UINT height = bitmap.GetHeight();

        BITMAPINFOHEADER bi = { 0 };
        bi.biSize = sizeof(BITMAPINFOHEADER);
        bi.biWidth = width;
        bi.biHeight = height;
        bi.biPlanes = 1;
        bi.biBitCount = 32;
        bi.biCompression = BI_RGB;

        DWORD dwSize = sizeof(BITMAPINFOHEADER) + (width * height * 4);
        HANDLE hGlobal = GlobalAlloc(GMEM_MOVEABLE, dwSize);
        if (!hGlobal)
        {
            return false;
        }

        void* pData = GlobalLock(hGlobal);
        if (!pData)
        {
            GlobalFree(hGlobal);
            return false;
        }

        memcpy(pData, &bi, sizeof(bi));

        Gdiplus::Rect rect(0, 0, width, height);
        Gdiplus::BitmapData bmpData;

        bitmap.LockBits(&rect, Gdiplus::ImageLockModeRead, PixelFormat32bppRGB, &bmpData);

        BYTE* pDestPixels = (BYTE*)pData + sizeof(BITMAPINFOHEADER);
        for (UINT y = 0; y < height; ++y)
        {
            memcpy(pDestPixels + (height - 1 - y) * (width * 4),
                (BYTE*)bmpData.Scan0 + (y * bmpData.Stride),
                width * 4);
        }

        bitmap.UnlockBits(&bmpData);
        GlobalUnlock(hGlobal);

        if (!SetClipboardData(CF_DIB, hGlobal))
        {
            GlobalFree(hGlobal);
            return false;
        }

        return true;
    }
}
