#include "pch.h"
#include "ClipboardDataHelper.h"
#include "HashHelper.h"

std::vector<UINT> ClipboardDataHelper::SupportedClipboardFormats;
std::vector<UINT> ClipboardDataHelper::RequiredClipboardFormats;   // Not used

// Call it before using this class
void ClipboardDataHelper::InitializeClipboardFormats()
{
    SupportedClipboardFormats = {
        CF_UNICODETEXT,
        CF_BITMAP,
        RegisterClipboardFormat(L"Rich Text Format"),
        RegisterClipboardFormat(L"HTML Format"),
        RegisterClipboardFormat(L"PNG"),
        RegisterClipboardFormat(L"image/png")
    };
    RequiredClipboardFormats = {
        CF_UNICODETEXT,
        RegisterClipboardFormat(L"PNG")
    };
}

void ClipboardDataHelper::MakeDataCopy(DataItemsRef destination, DataItemsRef clipboardData)
{
    FreeClipboardData(destination);

    for (const auto& item : clipboardData)
    {
        FormatDataItem newItem = {};
        newItem.format = item.format;

        if (item.format == CF_BITMAP) {
            HBITMAP hBitmap = (HBITMAP)item.data;
            BITMAP bitmapInfo = {};
            std::vector<BYTE> pixelData;

            if (GetBitmapAndPixels(hBitmap, bitmapInfo, pixelData))
            {
                size_t totalSize = sizeof(bitmapInfo) + pixelData.size();
                newItem.data = malloc(totalSize);
                memcpy(newItem.data, &bitmapInfo, sizeof(bitmapInfo));
                memcpy((BYTE*)newItem.data + sizeof(bitmapInfo), pixelData.data(), pixelData.size());
                newItem.size = totalSize;
                newItem.hash = HashHelper::ComputeSHA256(newItem.data, totalSize);
            }
        }
        else {
            newItem.data = malloc(item.size);
            memcpy(newItem.data, item.data, item.size);
            newItem.size = item.size;
            newItem.hash = HashHelper::ComputeSHA256(item.data, item.size);
        }

        destination.push_back(newItem);
    }
}

bool ClipboardDataHelper::CompareClipboardData(DataItemsRef previousData, DataItemsRef newData)
{
    if (previousData.size() != newData.size())
    {
        return false;
    }
    for (const auto& newItem : newData)
    {
        void* data = GlobalLock(newItem.data);
        auto* prewItem = GetDataByFormat(previousData, newItem.format);

        if (!prewItem || prewItem->size != newItem.size || memcmp(prewItem->data, newItem.data, newItem.size))
        {
            GlobalUnlock(data);
            return false;
        }

        GlobalUnlock(data);
    }
    return true;
}

bool ClipboardDataHelper::IsFormatSupported(const UINT& format)
{
    for (const auto& supportedFormat : SupportedClipboardFormats)
    {
        if (supportedFormat == format)
        {
            return true;
        }
    }
    return false;
}

FormatDataItem* ClipboardDataHelper::GetDataByFormat(DataItemsRef clipboardData, const UINT& format)
{
    for (auto& item : clipboardData)
    {
        if (item.format == format)
        {
            return &item;
        }
    }
    return nullptr;
}

bool ClipboardDataHelper::GetBitmapAndPixels(HBITMAP hBitmap, BITMAP& outBitmap, std::vector<BYTE>& outPixelData)
{
    if (!hBitmap) {
        return false;
    }

    if (GetObject(hBitmap, sizeof(BITMAP), &outBitmap) == 0) {
        return false;
    }

    HDC hdcScreen = GetDC(NULL);
    if (!hdcScreen) {
        return false;
    }
    HDC hdcMem = CreateCompatibleDC(hdcScreen);
    if (!hdcMem) {
        ReleaseDC(NULL, hdcScreen);
        return false;
    }

    BITMAPINFO bmi = {};
    bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    bmi.bmiHeader.biWidth = outBitmap.bmWidth;
    bmi.bmiHeader.biHeight = -abs(outBitmap.bmHeight);   // Using -height to flip the bitmap
    bmi.bmiHeader.biPlanes = 1;
    bmi.bmiHeader.biBitCount = outBitmap.bmBitsPixel;
    bmi.bmiHeader.biCompression = BI_RGB;

    if (GetDIBits(hdcMem,
        hBitmap,
        0,
        outBitmap.bmHeight,
        NULL,   // Use NULL just to get info and size
        &bmi,
        DIB_RGB_COLORS) == 0)
    {
        DeleteDC(hdcMem);
        ReleaseDC(NULL, hdcScreen);
        return false;
    }

    if (bmi.bmiHeader.biSizeImage == 0) {
        // If image size is 0, calculate it manually
        DWORD dwBytesPerRow = ((bmi.bmiHeader.biWidth * bmi.bmiHeader.biBitCount + 31) / 32) * 4;
        bmi.bmiHeader.biSizeImage = dwBytesPerRow * abs(bmi.bmiHeader.biHeight);
    }

    if (bmi.bmiHeader.biSizeImage == 0) {
        DeleteDC(hdcMem);
        ReleaseDC(NULL, hdcScreen);
        return false;
    }

    outPixelData.resize(bmi.bmiHeader.biSizeImage);

    if (GetDIBits(hdcMem,
        hBitmap,
        0,
        outBitmap.bmHeight,
        outPixelData.data(),   // Pixel buffer pointer
        &bmi,
        DIB_RGB_COLORS) == 0)
    {
        outPixelData.clear();
        DeleteDC(hdcMem);
        ReleaseDC(NULL, hdcScreen);
        return false;
    }

    DeleteDC(hdcMem);
    ReleaseDC(NULL, hdcScreen);
    return true;
}

void ClipboardDataHelper::FreeClipboardData(DataItemsRef clipboardData)
{
    for (auto& item : clipboardData)
    {
        free(item.data);
        free(item.hash);
    }
    clipboardData.clear();
}
