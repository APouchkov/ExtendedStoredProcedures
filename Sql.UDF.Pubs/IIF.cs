using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

public partial class Pub
{
    /// <summary>
    /// В зависимости от истинности условия возвращаются значения TrueValue или FalseValue
    /// </summary>
    [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static System.Object IIF(Object Arg1, Object Arg2, SqlString Condition, Object TrueValue, Object FalseValue)
    {
        if (Arg1.Equals(DBNull.Value) || Arg2.Equals(DBNull.Value)) { return DBNull.Value; };
        if (Arg1.GetType() != Arg2.GetType()) { return DBNull.Value; };
        int Compare = ((IComparable)Arg1).CompareTo(Arg2);

        switch (Condition.Value)
        {
            case "=": if (Compare == 0) { return TrueValue; } else { return FalseValue; };
            case "<>": if (Compare != 0) { return TrueValue; } else { return FalseValue; };
            case "!=": if (Compare != 0) { return TrueValue; } else { return FalseValue; };
            case ">": if (Compare > 0) { return TrueValue; } else { return FalseValue; };
            case "<": if (Compare < 0) { return TrueValue; } else { return FalseValue; };
            case ">=": if (Compare >= 0) { return TrueValue; } else { return FalseValue; };
            case "<=": if (Compare <= 0) { return TrueValue; } else { return FalseValue; };
        }
        return DBNull.Value;
    }
};