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
    [SqlFunction(Name = "IIF", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
    public static System.Object IIF(Object Arg1, Object Arg2, String ACondition, Object ATrueValue, Object AFalseValue)
    {
        if (Arg1.Equals(DBNull.Value) || Arg2.Equals(DBNull.Value)) { return DBNull.Value; };
        if (Arg1.GetType() != Arg2.GetType()) { return DBNull.Value; };
        int Compare = ((IComparable)Arg1).CompareTo(Arg2);

        switch (ACondition)
        {
            case "=" : if (Compare == 0) { return ATrueValue; } else { return AFalseValue; };
            case "<>": if (Compare != 0) { return ATrueValue; } else { return AFalseValue; };
            case "!=": if (Compare != 0) { return ATrueValue; } else { return AFalseValue; };
            case ">" : if (Compare > 0)  { return ATrueValue; } else { return AFalseValue; };
            case "<" : if (Compare < 0)  { return ATrueValue; } else { return AFalseValue; };
            case ">=": if (Compare >= 0) { return ATrueValue; } else { return AFalseValue; };
            case "<=": if (Compare <= 0) { return ATrueValue; } else { return AFalseValue; };
        }
        return DBNull.Value;
    }
};