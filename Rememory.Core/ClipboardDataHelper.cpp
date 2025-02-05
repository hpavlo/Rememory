#include "pch.h"
#include "ClipboardDataHelper.h"
#include "HashHelper.h"

std::vector<UINT> ClipboardDataHelper::SupportedClipboardFormats;
std::vector<UINT> ClipboardDataHelper::RequiredClipboardFormats;

// Call it before using this class
void ClipboardDataHelper::InitializeClipboardFormats()
{
    SupportedClipboardFormats = {
        CF_UNICODETEXT,
        RegisterClipboardFormat(L"Rich Text Format"),
        RegisterClipboardFormat(L"HTML Format"),
        RegisterClipboardFormat(L"PNG")
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
        newItem.data = malloc(item.size);
        memcpy(newItem.data, item.data, item.size);
        newItem.size = item.size;
        newItem.hash = HashHelper::ComputeSHA256(item.data, item.size);
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

void ClipboardDataHelper::FreeClipboardData(DataItemsRef clipboardData)
{
    for (auto& item : clipboardData)
    {
        free(item.data);
        free(item.hash);
    }
    clipboardData.clear();
}
