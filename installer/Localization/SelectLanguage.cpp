// Native MSI localization. Only temporary session rows are added; the cached MSI is unchanged.
// Deliberately imports only Windows APIs, so no C++ runtime or .NET installation is required.
typedef unsigned int UINT;
typedef unsigned long DWORD;
typedef unsigned int MSIHANDLE;
typedef unsigned short LANGID;
extern "C" {
__declspec(dllimport) LANGID __stdcall GetUserDefaultUILanguage();
__declspec(dllimport) MSIHANDLE __stdcall MsiGetActiveDatabase(MSIHANDLE);
__declspec(dllimport) UINT __stdcall MsiDatabaseOpenViewW(MSIHANDLE, const wchar_t*, MSIHANDLE*);
__declspec(dllimport) UINT __stdcall MsiViewExecute(MSIHANDLE, MSIHANDLE);
__declspec(dllimport) UINT __stdcall MsiViewFetch(MSIHANDLE, MSIHANDLE*);
__declspec(dllimport) UINT __stdcall MsiViewModify(MSIHANDLE, int, MSIHANDLE);
__declspec(dllimport) UINT __stdcall MsiViewClose(MSIHANDLE);
__declspec(dllimport) UINT __stdcall MsiCloseHandle(MSIHANDLE);
__declspec(dllimport) MSIHANDLE __stdcall MsiCreateRecord(UINT);
__declspec(dllimport) UINT __stdcall MsiRecordSetStringW(MSIHANDLE, UINT, const wchar_t*);
__declspec(dllimport) UINT __stdcall MsiRecordSetInteger(MSIHANDLE, UINT, int);
__declspec(dllimport) UINT __stdcall MsiSetPropertyW(MSIHANDLE, const wchar_t*, const wchar_t*);
}
struct LocalizedRow { const wchar_t* fields[12]; };
struct LocalizedTable {
    const wchar_t* query;
    UINT fieldCount, integerMask;
    const LocalizedRow* rows;
    UINT rowCount;
};
struct LocalizedLanguage { const wchar_t* id; const LocalizedTable* tables; UINT tableCount; };
#include "LanguageData.h"

static int Integer(const wchar_t* text)
{
    bool negative = *text == L'-';
    if (negative) ++text;
    int number = 0;
    while (*text >= L'0' && *text <= L'9') { number = number * 10 + (*text - L'0'); ++text; }
    return negative ? -number : number;
}

extern "C" __declspec(dllexport) UINT __stdcall SelectBoardLanguage(MSIHANDLE session)
{
    // LANGID uses the low 10 bits for the primary language. Unknown languages use English.
    LANGID detected = GetUserDefaultUILanguage();
    UINT index = 0;
    if ((detected & 0x3ff) == 0x11) index = 1; // Japanese
    else if ((detected & 0x3ff) == 0x04)
        index = detected == 0x0404 || detected == 0x0c04 || detected == 0x1404 || detected == 0x7c04 ? 3 : 2;
    const LocalizedLanguage& language = Languages[index];
    MSIHANDLE database = MsiGetActiveDatabase(session);
    if (!database) return 1603;
    UINT result = 0;
    for (UINT t = 0; t < language.tableCount && !result; ++t)
    {
        const LocalizedTable& table = language.tables[t];
        MSIHANDLE view = 0;
        result = MsiDatabaseOpenViewW(database, table.query, &view);
        if (!result) result = MsiViewExecute(view, 0);
        // The UI and execute sequences can share a database. Avoid inserting the same rows twice.
        MSIHANDLE existing = 0;
        if (!result)
        {
            UINT fetched = MsiViewFetch(view, &existing);
            if (fetched == 0)
            {
                MsiCloseHandle(existing); MsiViewClose(view); MsiCloseHandle(view); continue;
            }
            if (fetched != 259) result = fetched;
        }
        for (UINT r = 0; r < table.rowCount && !result; ++r)
        {
            MSIHANDLE record = MsiCreateRecord(table.fieldCount);
            if (!record) { result = 1603; break; }
            for (UINT f = 0; f < table.fieldCount && !result; ++f)
            {
                const wchar_t* value = table.rows[r].fields[f];
                if (!value) continue;
                result = (table.integerMask & (1u << f)) ? MsiRecordSetInteger(record, f + 1, Integer(value)) :
                    MsiRecordSetStringW(record, f + 1, value);
            }
            if (!result) result = MsiViewModify(view, 7, record); // MSIMODIFY_INSERT_TEMPORARY
            MsiCloseHandle(record);
        }
        if (view) { MsiViewClose(view); MsiCloseHandle(view); }
    }
    MsiCloseHandle(database);
    if (!result) result = MsiSetPropertyW(session, L"ProductLanguage", language.id);
    if (!result) result = MsiSetPropertyW(session, L"BOARD_UI_LANGUAGE", language.id);
    return result ? 1603 : 0;
}
