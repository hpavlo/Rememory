#pragma once
#include <vector>

struct FormatDataItem
{
    UINT format;
    void* data;
    SIZE_T size;
    BYTE* hash;

    FormatDataItem() {}
    FormatDataItem(UINT frmt, void* hData)
        : format(frmt), data(hData), size(GlobalSize(hData)) {}
};

struct ClipboardDataInfo
{
    UINT formatCount;
    FormatDataItem* firstItem;
    WCHAR* ownerPath;
    LONG iconLength;
    BYTE* iconPixels;
};

using DataItemsRef = std::vector<FormatDataItem>&;

class ClipboardDataHelper
{
public:
	static void InitializeClipboardFormats();
    static void MakeDataCopy(DataItemsRef destination, DataItemsRef clipboardData);
    static bool CompareClipboardData(DataItemsRef previousClipboardData, DataItemsRef newClipboardData);
    static bool IsFormatSupported(const UINT& format);
    static FormatDataItem* GetDataByFormat(DataItemsRef clipboardData, const UINT& format);

private:
	static std::vector<UINT> SupportedClipboardFormats;
	static std::vector<UINT> RequiredClipboardFormats;

	ClipboardDataHelper();
    static bool GetBitmapAndPixels(HBITMAP hBitmap, BITMAP& outBitmap, std::vector<BYTE>& outPixelData);
    static void FreeClipboardData(DataItemsRef clipboardData);
};
