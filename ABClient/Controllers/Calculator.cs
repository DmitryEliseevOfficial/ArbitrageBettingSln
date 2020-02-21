using System;

namespace ABClient.Controllers
{
    public static class  Calculator
    {
        //считаем от общей суммы
        public static CalculatorData CalculatorBet(double coeff1, double coeff2, int maxBet)
        {
            int c1 = (int)(coeff2 / (coeff1 + coeff2) * maxBet);
            int c2 = maxBet - c1;

            CalculatorData cld = new CalculatorData();
            cld.bet1 = c1;
            cld.bet2 = c2;
            cld.coeff1 = coeff1;
            cld.coeff2 = coeff2;
            cld.Staf = maxBet;

            cld = ComputeProfit(cld);
            return cld;
        }

        public static CalculatorData CalculatorBetShoulderOne(double coeff1, double coeff2, int CurrentBet)
        {
            int c1 = (int)(CurrentBet);
            int c2 = (int)((coeff1*c1)/coeff2);

            CalculatorData cld = new CalculatorData();
            cld.bet1 = c1;
            cld.bet2 = c2;
            cld.coeff1 = coeff1;
            cld.coeff2 = coeff2;
            cld.Staf = c1+c2;

            cld = ComputeProfit(cld);
            return cld;
        }

        public static CalculatorData CalculatorBetShoulderTwo(double coeff1, double coeff2, int CurrentBet)
        {
            int c2 = (int)(CurrentBet);
            int c1 =(int) ((coeff2 * CurrentBet) - CurrentBet);
           

            CalculatorData cld = new CalculatorData();
            cld.bet1 = c1;
            cld.bet2 = c2;
            cld.coeff1 = coeff1;
            cld.coeff2 = coeff2;
            cld.Staf = c1 + c2;

            cld = ComputeProfit(cld);
            return cld;
        }


        public static CalculatorData ComputeProfit(CalculatorData cld)
        {
            cld.profit1 = cld.coeff1 * cld.bet1 - cld.Staf;
            cld.profit2 = cld.coeff2 * cld.bet2 - cld.Staf;

            cld.ForkProfit= (1 / ((1 / cld.coeff1) + (1 / cld.coeff2)) - 1) * 100;


            cld.IsFork = CheckFork(cld.coeff1, cld.coeff2);

            cld.Mean = (cld.profit1 + cld.profit2) / 2;
            return cld;
        }

        public static int aRound(double digit)
        {
            if (digit < 0)
                return 0;
            if (digit < 100)
                return (int)digit;
            if (digit < 1000)
            {
                int small = digit.SmallBit();
                var round = Round(small);
                return (int)digit - small + round;
            }
            return  ((int)(digit / 100) * 100);
        }


        private static int Round(int digit)
        {
            if (digit >= 0 && digit < 50)
                return 0;
            if (digit > 50 && digit < 100)
                return 50;
            return digit;
        }


        public static int SmallBit(this double digit)
        {
            int x = (int)digit;
            if (x < 100)
                return x;            
            var z = x.ToString().Remove(x.ToString().Length - 2) + "00";
            return x - Convert.ToInt32(z);           
        }


        private static bool CheckFork(double kofOne, double kofTwo)
        {
            if (kofOne != 0 || kofTwo != 0)
            {
                var rez = ((1 / kofOne) + (1 / kofTwo));
                if (rez < 1)
                {
                    return true;
                }
            }

            return false;
        }


    }
}
