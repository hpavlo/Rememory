#pragma once
#include "pch.h"
#include <functional>
#include <filesystem>
#include "FormatManager.g.h"
#include "ClipboardMonitor.h"

#define CF_RTF RegisterClipboardFormat(L"Rich Text Format")
#define CF_HTML RegisterClipboardFormat(L"HTML Format")
#define CF_PNG RegisterClipboardFormat(L"PNG")
#define CF_IMAGEPNG RegisterClipboardFormat(L"image/png")

namespace winrt::RememoryCore::implementation
{
    struct FormatManager : FormatManagerT<FormatManager>
    {
    private:
        struct FormatRule {
            std::vector<UINT> clipboardIds;
            std::function<bool(HANDLE, size_t, ClipboardData*)> copyFromClipboardFunction;
            std::function<winrt::Windows::Foundation::IAsyncOperation<winrt::hstring>(std::filesystem::path, ClipboardFormat, const ClipboardData*)> saveToFileFunction;
            std::function<bool(UINT, const winrt::hstring&)> loadToClipboardFunction;
        };

        static inline const std::unordered_map<ClipboardFormat, winrt::hstring> formatNames
        {
            { ClipboardFormat::Text, L"CF_UNICODETEXT" },
            { ClipboardFormat::Bitmap, L"CF_BITMAP" },
            { ClipboardFormat::Files, L"CF_HDROP" },
            { ClipboardFormat::Rtf, L"Rich Text Format" },
            { ClipboardFormat::Html, L"HTML Format" },
            { ClipboardFormat::Png, L"PNG" },
        };

        static bool GetGeneralDataCopy(HANDLE hData, size_t maxDataSize, ClipboardData* clipboardData);
        static bool GetFilesDataCopy(HANDLE hData, size_t maxDataSize, ClipboardData* clipboardData);
        static bool GetBitmapDataCopy(HANDLE hData, size_t maxDataSize, ClipboardData* clipboardData);

        static winrt::Windows::Foundation::IAsyncOperation<winrt::hstring> SaveGeneralDataToFile(std::filesystem::path rootHistoryFolder, ClipboardFormat format, const ClipboardData* clipboardData);
        static winrt::Windows::Foundation::IAsyncOperation<winrt::hstring> SaveBitmapToFile(std::filesystem::path rootHistoryFolder, ClipboardFormat format, const ClipboardData* clipboardData);

        static bool LoadGeneralDataToClipboard(UINT formatId, const winrt::hstring& data);
        static bool LoadUnicodeToClipboard(UINT formatId, const winrt::hstring& data);
        static bool LoadFilesToClipboard(UINT formatId, const winrt::hstring& data);
        static bool LoadImageToClipboard(UINT formatId, const winrt::hstring& data);
        static bool LoadBitmapToClipboard(UINT formatId, const winrt::hstring& data);

    public:
        static inline const std::vector<std::pair<ClipboardFormat, FormatRule>> ClipboardFormatRules
        {
            { ClipboardFormat::Files,  { { CF_HDROP },             GetFilesDataCopy,     nullptr,                  LoadFilesToClipboard       } },
            { ClipboardFormat::Png,    { { CF_PNG, CF_IMAGEPNG },  GetGeneralDataCopy,   SaveGeneralDataToFile,    LoadImageToClipboard       } },
            { ClipboardFormat::Html,   { { CF_HTML },              GetGeneralDataCopy,   SaveGeneralDataToFile,    LoadGeneralDataToClipboard } },
            { ClipboardFormat::Rtf,    { { CF_RTF },               GetGeneralDataCopy,   SaveGeneralDataToFile,    LoadGeneralDataToClipboard } },
            { ClipboardFormat::Bitmap, { { CF_BITMAP },            GetBitmapDataCopy,    SaveBitmapToFile,         LoadBitmapToClipboard      } },
            { ClipboardFormat::Text,   { { CF_UNICODETEXT },       GetGeneralDataCopy,   nullptr,                  LoadUnicodeToClipboard     } }
        };

        static const FormatRule* GetRule(ClipboardFormat format)
        {
            auto it = std::find_if(ClipboardFormatRules.begin(), ClipboardFormatRules.end(),
                [format](const auto& pair) { return pair.first == format; });

            if (it != ClipboardFormatRules.end()) {
                return &it->second;
            }

            throw winrt::hresult_invalid_argument(L"This format is not supported: " + FormatToName(format));
        }

        // The root directory name within the application's local data folder where clipboard data is stored.
        static const winrt::hstring& RootHistoryFolderName() {
            static winrt::hstring name{ L"History" };
            return name;
        }

        // The subfolder name within the history root for storing RTF format files.
        static const winrt::hstring& RtfFolderName() {
            static winrt::hstring name{ L"RtfFormat" };
            return name;
        }

        // The subfolder name within the history root for storing HTML format files.
        static const winrt::hstring& HtmlFolderName() {
            static winrt::hstring name{ L"HtmlFormat" };
            return name;
        }

        // The subfolder name within the history root for storing PNG format files.
        static const winrt::hstring& PngFolderName() {
            static winrt::hstring name{ L"PngFormat" };
            return name;
        }

        // The subfolder name within the history root for storing Bitmap format files.
        static const winrt::hstring& BitmapFolderName() {
            static winrt::hstring name{ L"BitmapFormat" };
            return name;
        }

        // The delimiter used to join multiple file paths into a single string for storage or transport.
        static const winrt::hstring& FilePathsSeparator() {
            static winrt::hstring separator{ L"|" };
            return separator;
        }

        static winrt::hstring FormatToName(ClipboardFormat format);
        static ClipboardFormat FormatFromName(winrt::hstring formatName);
        static winrt::hstring GenerateFileName(ClipboardFormat format);
        static winrt::hstring GetFormatFolderName(ClipboardFormat format);
    };
}

namespace winrt::RememoryCore::factory_implementation
{
    struct FormatManager : FormatManagerT<FormatManager, implementation::FormatManager> {};
}
