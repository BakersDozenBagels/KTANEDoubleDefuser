namespace DoubleDefuser
{
    public static class Extensions
    {
        internal static DoubleDefuserScript.Direction AsDirection(this string s)
        {
            switch(s)
            {
                case "tl":
                    return DoubleDefuserScript.Direction.TL;
                case "bl":
                    return DoubleDefuserScript.Direction.BL;
                case "tr":
                    return DoubleDefuserScript.Direction.TR;
                case "br":
                    return DoubleDefuserScript.Direction.BR;
            }
            return DoubleDefuserScript.Direction.None;
        }

        internal static DoubleDefuserScript.Color AsColor(this string s, bool byPosition)
        {
            if(byPosition)
            {
                switch(s)
                {
                    case "red":
                        return DoubleDefuserScript.Color.CR;
                    case "green":
                        return DoubleDefuserScript.Color.CG;
                    case "blue":
                        return DoubleDefuserScript.Color.CB;
                    case "yellow":
                        return DoubleDefuserScript.Color.CY;
                }
            }
            else
                switch(s)
                {
                    case "red":
                        return DoubleDefuserScript.Color.LR;
                    case "green":
                        return DoubleDefuserScript.Color.LG;
                    case "blue":
                        return DoubleDefuserScript.Color.LB;
                    case "yellow":
                        return DoubleDefuserScript.Color.LY;
                }
            return DoubleDefuserScript.Color.None;
        }

        internal static DoubleDefuserScript.Key AsKey(this string s)
        {
            switch(s)
            {
                case "s":
                    return DoubleDefuserScript.Key.S;
                case "f":
                    return DoubleDefuserScript.Key.F;
                case "j":
                    return DoubleDefuserScript.Key.J;
                case "l":
                    return DoubleDefuserScript.Key.L;
            }
            return DoubleDefuserScript.Key.None;
        }

        internal static UnityEngine.Color AsRGBColor(this DoubleDefuserScript.Color c)
        {
            switch(c)
            {
                case DoubleDefuserScript.Color.CB:
                case DoubleDefuserScript.Color.LB:
                    return new UnityEngine.Color(0, 0, 1);
                case DoubleDefuserScript.Color.CR:
                case DoubleDefuserScript.Color.LR:
                    return new UnityEngine.Color(1, 0, 0);
                case DoubleDefuserScript.Color.CG:
                case DoubleDefuserScript.Color.LG:
                    return new UnityEngine.Color(0, 1, 0);
                case DoubleDefuserScript.Color.CY:
                case DoubleDefuserScript.Color.LY:
                    return new UnityEngine.Color(1, 1, 0);
            }
            return new UnityEngine.Color(1, 1, 1);
        }
    }
}