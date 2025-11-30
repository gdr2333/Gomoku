using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gomoku
{
    internal enum SquareStatus : byte
    {
        Empty,
        UsedByBlack,
        UsedByWhite
    }
}
