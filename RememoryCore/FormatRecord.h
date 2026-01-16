#pragma once
#include "pch.h"
#include "FormatRecord.g.h"
#include "FormatManager.h"

namespace winrt::RememoryCore::implementation
{
    struct FormatRecord : FormatRecordT<FormatRecord>
    {
        FormatRecord() = default;

        ClipboardFormat Format() const { return m_format; }
        void Format(ClipboardFormat value) { m_format = value; }

        winrt::hstring Data() const { return m_data; }
        void Data(winrt::hstring const& value) { m_data = value; }

        winrt::Windows::Storage::Streams::IBuffer Hash() const { return m_hash; }
        void Hash(winrt::Windows::Storage::Streams::IBuffer const& value) { m_hash = value; }

    private:
        ClipboardFormat m_format {};
        winrt::hstring m_data {};
        winrt::Windows::Storage::Streams::IBuffer m_hash { nullptr };
    };
}

namespace winrt::RememoryCore::factory_implementation
{
    struct FormatRecord : FormatRecordT<FormatRecord, implementation::FormatRecord> {};
}
