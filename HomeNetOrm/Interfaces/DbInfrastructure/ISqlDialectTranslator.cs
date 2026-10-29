using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Interfaces.DbInfrastructure
{
    public interface ISqlDialectTranslator
    {
        string FormatTable(TableSchema schema);
        string FormatColumnName(string columnName);
        string FormatLimit(int limit, int? offset = null);

        // 🆕 Теперь это часть общего контракта для всех СУБД:
        string FormatCastToText(string columnName);
        string FormatCountAll();
        string FormatCountOne();

        string Translate(CmdType cmd);
        string Translate(PhraseType phrase);
        string Translate(OpType op);
        string Translate(TriggerType trig);
        string FormatReturningId(string v);
        string FormatNow();
    }
}