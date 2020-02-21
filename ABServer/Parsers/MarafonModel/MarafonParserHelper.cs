using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ABShared;
using ABShared.Enum;
using AngleSharp.Dom;
using Newtonsoft.Json.Linq;

namespace ABServer.Parsers.MarafonModel
{
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    internal static class MarafonParserHelper
    {
        public static void Football(IHtmlCollection<IElement> collection, Bet bet)
        {
            foreach (IElement element in collection)
            {
                var stakeValue = element.Attributes["data-selection-key"].Value ?? "";
                if(stakeValue=="")
                    continue;
                if(stakeValue.EndsWith(".yes")
                    || stakeValue.EndsWith(".no")
                    || stakeValue.EndsWith(".odd")
                    || stakeValue.EndsWith(".even")
                    || stakeValue.Contains("Asian_")
                    || stakeValue.Contains("From_{0}_to_{1}"))
                    continue;

                if (stakeValue.EndsWith("@Match_Result.1")
                    || stakeValue.EndsWith("@Match_Result0.1"))
                {
                    SetCoef(bet, BetNumber._1, element);
                }
                else if (stakeValue.EndsWith("@Match_Result.draw")
                    || stakeValue.EndsWith("@Match_Result0.draw"))
                {   
                    SetCoef(bet, BetNumber._X, element);

                }
                else if (stakeValue.EndsWith("@Match_Result.3")
                    || stakeValue.EndsWith("@Match_Result0.3"))
                {
                    SetCoef(bet, BetNumber._2, element);

                }
                else if (stakeValue.EndsWith("@Result.HD")
                    || stakeValue.EndsWith("@Result0.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element);

                }
                else if (stakeValue.EndsWith("@Result.HA")
                    || stakeValue.EndsWith("@Result0.HA"))
                {
                    SetCoef(bet, BetNumber._12, element);
                }
                else if (stakeValue.EndsWith("@Result.AD")
                    || stakeValue.EndsWith("@Result0.AD"))
                {
                    SetCoef(bet, BetNumber._X2, element);
                }

                //Первый тайм
                else if (stakeValue.EndsWith("@Result_-_1st_Half.RN_H")
                    || stakeValue.EndsWith("@Result_-_1st_Half0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@Result_-_1st_Half.RN_D")
                    || stakeValue.EndsWith("@Result_-_1st_Half0.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@Result_-_1st_Half.RN_A")
                    || stakeValue.EndsWith("@Result_-_1st_Half0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time1);
                }

               
                else if (stakeValue.EndsWith("@Result_-_1st_Half0.HD")
                    || stakeValue.EndsWith("@Result_-_1st_Half.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@Result_-_1st_Half0.HA")
                    || stakeValue.EndsWith("@Result_-_1st_Half.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@Result_-_1st_Half0.AD")
                    || stakeValue.EndsWith("@Result_-_1st_Half.AD"))
                {
                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Time1);
                }

                //Второй тайм
                else if (stakeValue.EndsWith("@Result_-_2nd_Half.RN_H")
                    || stakeValue.EndsWith("@Result_-_2nd_Half0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@Result_-_2nd_Half.RN_D")
                    || stakeValue.EndsWith("@Result_-_2nd_Half0.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@Result_-_2nd_Half.RN_A")
                    || stakeValue.EndsWith("@Result_-_2nd_Half0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time2);
                }


                else if (stakeValue.EndsWith("@Result_-_2nd_Half0.HD")
                    || stakeValue.EndsWith("@Result_-_2nd_Half.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@Result_-_2nd_Half0.HA")
                    || stakeValue.EndsWith("@Result_-_2nd_Half.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@Result_-_2nd_Half0.AD")
                    || stakeValue.EndsWith("@Result_-_2nd_Half.AD"))
                {
                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Time2);
                }

                //Форы
                else if (stakeValue.Contains("@To_Win_Match_With_Handicap")
                    || stakeValue.Contains("@Draw_No_Bet"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st);
                }

                else if (stakeValue.Contains("@To_Win_1st_Half_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st,SportTimePart.Time1);
                }

                else if (stakeValue.Contains("@To_Win_2nd_Half_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time2);
                }

                // ИТоталы

                else if (stakeValue.Contains("@Total_Goals_(First_Team)"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st,true);
                    AddStake(bet, st);
                }
                else if (stakeValue.Contains("@Total_Goals_(Second_Team)"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st);
                }
                //Тоталы. Идем на оборот. Иначе могу не туда попасть

                else if (stakeValue.Contains("@Total_Goals_-_2nd_Half"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st,SportTimePart.Time2);
                }

                else if (stakeValue.Contains("@Total_Goals_-_1st_Half"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time1);
                }

                else if (stakeValue.Contains("@Total_Goals"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st);
                }

            }
        }

        public static void Hockey(IHtmlCollection<IElement> collection, Bet bet)
        {
            foreach (IElement element in collection)
            {
                var stakeValue = element.Attributes["data-selection-key"].Value ?? "";
                if (stakeValue == "")
                    continue;
                if (stakeValue.EndsWith(".yes")
                    || stakeValue.EndsWith(".no")
                    || stakeValue.EndsWith(".odd")
                    || stakeValue.EndsWith(".even")
                    || stakeValue.Contains("Asian_"))
                    continue;

                if (stakeValue.EndsWith("@Result.1")
                    || stakeValue.EndsWith("@Result0.1"))
                {
                    SetCoef(bet, BetNumber._1, element);
                }
                else if (stakeValue.EndsWith("@Result.draw")
                    || stakeValue.EndsWith("@Result0.draw"))
                {
                    SetCoef(bet, BetNumber._X, element);

                }
                else if (stakeValue.EndsWith("@Result.3")
                    || stakeValue.EndsWith("@Result0.3"))
                {
                    SetCoef(bet, BetNumber._2, element);

                }
                else if (stakeValue.EndsWith("@Result0.HD")
                    || stakeValue.EndsWith("@Result.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element);

                }
                else if (stakeValue.EndsWith("@Result0.HA")
                     || stakeValue.EndsWith("@Result.HA"))
                {
                    SetCoef(bet, BetNumber._12, element);
                }
                else if (stakeValue.EndsWith("@Result0.AD")
                    || stakeValue.EndsWith("@Result.AD"))
                {
                    SetCoef(bet, BetNumber._X2, element);
                }

                //Первый тайм
                else if (stakeValue.EndsWith("@Result_-_1st_Period.RN_H")
                    || stakeValue.EndsWith("@Result_-_1st_Period0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@Result_-_1st_Period.RN_D")
                    || stakeValue.EndsWith("@Result_-_1st_Period0.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@Result_-_1st_Period.RN_A")
                    || stakeValue.EndsWith("@Result_-_1st_Period0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time1);
                }


                else if (stakeValue.EndsWith("@Result_-_1st_Period0.HD")
                    || stakeValue.EndsWith("@Result_-_1st_Period.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@Result_-_1st_Period0.HA")
                    || stakeValue.EndsWith("@Result_-_1st_Period.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@Result_-_1st_Period0.AD")
                    || stakeValue.EndsWith("@Result_-_1st_Period.AD"))
                {
                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Time1);
                }


                //Второй тайм
                else if (stakeValue.EndsWith("@Result_-_2nd_Period.RN_H")
                    || stakeValue.EndsWith("@Result_-_2nd_Period0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@Result_-_2nd_Period.RN_D")
                    || stakeValue.EndsWith("@Result_-_2nd_Period0.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@Result_-_2nd_Period.RN_A")
                    || stakeValue.EndsWith("@Result_-_2nd_Period0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time2);
                }


                else if (stakeValue.EndsWith("@Result_-_2nd_Period0.HD")
                    || stakeValue.EndsWith("@Result_-_2nd_Period.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@Result_-_2nd_Period0.HA")
                    || stakeValue.EndsWith("@Result_-_2nd_Period.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@Result_-_2nd_Period0.AD")
                    || stakeValue.EndsWith("@Result_-_2nd_Period.AD"))
                {
                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Time2);
                }

                //Третий тайм
                else if (stakeValue.EndsWith("@Result_-_3rd_Period.RN_H")
                    || stakeValue.EndsWith("@Result_-_3rd_Period0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time3);
                }

                else if (stakeValue.EndsWith("@Result_-_3rd_Period.RN_D")
                    || stakeValue.EndsWith("@Result_-_3rd_Period0.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Time3);
                }

                else if (stakeValue.EndsWith("@Result_-_3rd_Period.RN_A")
                     || stakeValue.EndsWith("@Result_-_3rd_Period0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time3);
                }


                else if (stakeValue.EndsWith("@Result_-_3rd_Period0.HD")
                    || stakeValue.EndsWith("@Result_-_3rd_Period.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Time3);
                }

                else if (stakeValue.EndsWith("@Result_-_3rd_Period0.HA")
                    || stakeValue.EndsWith("@Result_-_3rd_Period.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Time3);
                }

                else if (stakeValue.EndsWith("@Result_-_3rd_Period0.AD")
                    || stakeValue.EndsWith("@Result_-_3rd_Period.AD"))
                {
                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Time3);
                }


                //Форы
                else if (stakeValue.Contains("@To_Win_Match_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st);
                }

                else if (stakeValue.Contains("@To_Win_1st_Period_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time1);
                }

                else if (stakeValue.Contains("@To_Win_2nd_Period_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time2);
                }

                else if (stakeValue.Contains("@To_Win_3rd_Period_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time3);
                }


                // ИТоталы

                else if (stakeValue.Contains("@Total_Goals_(First_Team)"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st);
                }
                else if (stakeValue.Contains("@Total_Goals_(Second_Team)"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st);
                }
                //Тоталы. Идем на оборот. Иначе могу не туда попасть

                else if (stakeValue.Contains("@Total_Goals_-_3rd_Period"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time3);
                }

                else if (stakeValue.Contains("@Total_Goals_-_2nd_Period"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time2);
                }

                else if (stakeValue.Contains("@Total_Goals_-_1st_Period"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time1);
                }

                else if (stakeValue.Contains("@Total_Goals"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st);
                }

            }
        }

        public static void Backetball(IHtmlCollection<IElement> collection, Bet bet)
        {
            foreach (IElement element in collection)
            {
                var stakeValue = element.Attributes["data-selection-key"].Value ?? "";
                if (stakeValue == "")
                    continue;
                if (stakeValue.EndsWith(".yes")
                    || stakeValue.EndsWith(".no")
                    || stakeValue.EndsWith(".odd")
                    || stakeValue.EndsWith(".even")
                    || stakeValue.Contains("Asian_"))
                    continue;

                if (stakeValue.EndsWith("@Match_Winner_Including_All_OT.HB_H"))
                {
                    SetCoef(bet, BetNumber._1, element);
                }

                else if (stakeValue.EndsWith("@Match_Winner_Including_All_OT.HB_A"))
                {
                    SetCoef(bet, BetNumber._2, element);

                }

                //Первый тайм
                else if (stakeValue.EndsWith("@1st_Quarter_Result0.RN_H")
                    || stakeValue.EndsWith("@1st_Quarter_Result.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@1st_Quarter_Result0.RN_D")
                    || stakeValue.EndsWith("@1st_Quarter_Result.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@1st_Quarter_Result0.RN_A")
                    || stakeValue.EndsWith("@1st_Quarter_Result.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time1);
                }


                else if (stakeValue.EndsWith("@1st_Quarter_Result.HD")
                    || stakeValue.EndsWith("@1st_Quarter_Result0.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@1st_Quarter_Result.HA")
                    || stakeValue.EndsWith("@1st_Quarter_Result0.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@1st_Quarter_Result.AD")
                    || stakeValue.EndsWith("@1st_Quarter_Result0.AD"))
                {
                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Time1);
                }


                //Второй тайм
                else if (stakeValue.EndsWith("@2nd_Quarter_Result0.RN_H")
                    || stakeValue.EndsWith("@2nd_Quarter_Result.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@2nd_Quarter_Result0.RN_D")
                    || stakeValue.EndsWith("@2nd_Quarter_Result.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@2nd_Quarter_Result0.RN_A")
                    || stakeValue.EndsWith("@2nd_Quarter_Result.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time2);
                }


                else if (stakeValue.EndsWith("@2nd_Quarter_Result.HD")
                    || stakeValue.EndsWith("@2nd_Quarter_Result0.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@2nd_Quarter_Result.HA")
                    || stakeValue.EndsWith("@2nd_Quarter_Result0.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@2nd_Quarter_Result.AD")
                    || stakeValue.EndsWith("@2nd_Quarter_Result0.AD"))
                {
                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Time2);
                }

                //Третий тайм
                else if (stakeValue.EndsWith("@3rd_Quarter_Result0.RN_H")
                    || stakeValue.EndsWith("@3rd_Quarter_Result.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time3);
                }

                else if (stakeValue.EndsWith("@3rd_Quarter_Result0.RN_D")
                    || stakeValue.EndsWith("@3rd_Quarter_Result.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Time3);
                }

                else if (stakeValue.EndsWith("@3rd_Quarter_Result0.RN_A")
                    || stakeValue.EndsWith("@3rd_Quarter_Result.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time3);
                }


                else if (stakeValue.EndsWith("@3rd_Quarter_Result.HD")
                    || stakeValue.EndsWith("@3rd_Quarter_Result0.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Time3);
                }

                else if (stakeValue.EndsWith("@3rd_Quarter_Result.HA")
                    || stakeValue.EndsWith("@3rd_Quarter_Result0.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Time3);
                }

                else if (stakeValue.EndsWith("@3rd_Quarter_Result.AD")
                    || stakeValue.EndsWith("@3rd_Quarter_Result0.AD"))
                {
                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Time3);
                }

                //Четвертый тайм
                else if (stakeValue.EndsWith("@4th_Quarter_Result0.RN_H")
                    || stakeValue.EndsWith("@4th_Quarter_Result.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time4);
                }

                else if (stakeValue.EndsWith("@4th_Quarter_Result0.RN_D")
                    || stakeValue.EndsWith("@4th_Quarter_Result.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Time4);
                }

                else if (stakeValue.EndsWith("@4th_Quarter_Result0.RN_A")
                    || stakeValue.EndsWith("@4th_Quarter_Result.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time4);
                }


                else if (stakeValue.EndsWith("@4th_Quarter_Result.HD")
                    || stakeValue.EndsWith("@4th_Quarter_Result0.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Time4);
                }

                else if (stakeValue.EndsWith("@4th_Quarter_Result.HA")
                    || stakeValue.EndsWith("@4th_Quarter_Result0.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Time4);
                }

                else if (stakeValue.EndsWith("@4th_Quarter_Result.AD")
                    || stakeValue.EndsWith("@4th_Quarter_Result0.AD"))
                {
                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Time4);
                }

                //Первая половина
                else if (stakeValue.EndsWith("@1st_Half_Result0.RN_H")
                    || stakeValue.EndsWith("@1st_Half_Result.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Half1);
                }

                else if (stakeValue.EndsWith("@1st_Half_Result0.RN_D")
                    || stakeValue.EndsWith("@1st_Half_Result.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Half1);
                }

                else if (stakeValue.EndsWith("@1st_Half_Result0.RN_A")
                    || stakeValue.EndsWith("@1st_Half_Result.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Half1);
                }


                else if (stakeValue.EndsWith("@1st_Half_Result.HD")
                    || stakeValue.EndsWith("@1st_Half_Result0.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Half1);
                }

                else if (stakeValue.EndsWith("@1st_Half_Result0.HA")
                    || stakeValue.EndsWith("@1st_Half_Result.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Half1);
                }

                else if (stakeValue.EndsWith("@1st_Half_Result.AD")
                    || stakeValue.EndsWith("@1st_Half_Result0.AD"))
                {
                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Half1);
                }

                //Вторая половина
                else if (stakeValue.EndsWith("@2nd_Half_Result0.RN_H")
                    || stakeValue.EndsWith("@2nd_Half_Result.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Half2);
                }

                else if (stakeValue.EndsWith("@2nd_Half_Result0.RN_D")
                    || stakeValue.EndsWith("@2nd_Half_Result.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Half2);
                }

                else if (stakeValue.EndsWith("@2nd_Half_Result0.RN_A")
                    || stakeValue.EndsWith("@2nd_Half_Result.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Half2);
                }


                else if (stakeValue.EndsWith("@2nd_Half_Result.HD")
                    || stakeValue.EndsWith("@2nd_Half_Result0.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Half2);
                }

                else if (stakeValue.EndsWith("@2nd_Half_Result0.HA")
                    || stakeValue.EndsWith("@2nd_Half_Result.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Half2);
                }

                else if (stakeValue.EndsWith("@2nd_Half_Result0.AD")
                    || stakeValue.EndsWith("@2nd_Half_Result.AD"))
                {
                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Half2);
                }


                //Форы
                else if (stakeValue.Contains("@To_Win_Match_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st);
                }

                else if (stakeValue.Contains("@To_Win_1st_Quarter_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time1);
                }

                else if (stakeValue.Contains("@To_Win_2nd_Quarter_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time2);
                }

                else if (stakeValue.Contains("@To_Win_3rd_Quarter_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time3);
                }

                else if (stakeValue.Contains("@To_Win_4th_Quarter_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time4);
                }


                else if (stakeValue.Contains("@To_Win_1st_Half_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Half1);
                }
                else if (stakeValue.Contains("@To_Win_2nd_Half_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Half2);
                }


                // ИТоталы

                else if (stakeValue.Contains("@Total_Points_(First_Team)_-_1st_Quarter"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st,SportTimePart.Time1);
                }
                else if (stakeValue.Contains("@Total_Points_(Second_Team)_-_1st_Quarter"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time1);
                }


                else if (stakeValue.Contains("@Total_Points_(First_Team)_-_2nd_Quarter"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time2);
                }
                else if (stakeValue.Contains("@Total_Points_(Second_Team)_-_2nd_Quarter"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time2);
                }
                
                else if (stakeValue.Contains("@Total_Points_(First_Team)_-_3rd_Quarter"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time3);
                }
                else if (stakeValue.Contains("@Total_Points_(Second_Team)_-_3rd_Quarter"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time3);
                }

                else if (stakeValue.Contains("@Total_Points_(First_Team)_-_4th_Quarter"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time4);
                }
                else if (stakeValue.Contains("@Total_Points_(Second_Team)_-_4th_Quarter"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time4);
                }


                else if (stakeValue.Contains("@Total_Points_(First_Team)_-_1st_Half"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Half1);
                }
                else if (stakeValue.Contains("@Total_Points_(Second_Team)_-_1st_Half"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Half1);
                }

                else if (stakeValue.Contains("@Total_Points_(First_Team)_-_2nd_Half"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Half2);
                }
                else if (stakeValue.Contains("@Total_Points_(Second_Team)_-_2nd_Half"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Half2);
                }

                else if (stakeValue.Contains("@Total_Points_(First_Team)"))
                {
                    Stake st = new Stake();
                    st.Team=ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st);
                }
                else if (stakeValue.Contains("@Total_Points_(Second_Team)"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st);
                }


                //Тоталы. Идем на оборот. Иначе могу не туда попасть
                else if (stakeValue.Contains("@Total_Points_-_2nd_Half"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Half2);
                }
                else if (stakeValue.Contains("@Total_Points_-_1st_Hal"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Half1);
                }

                else if (stakeValue.Contains("@Total_Points_-_4th_Quarter"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time4);
                }
                else if (stakeValue.Contains("@Total_Points_-_3rd_Quarter"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time3);
                }

                else if (stakeValue.Contains("@Total_Points_-_2nd_Quarter"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time2);
                }

                else if (stakeValue.Contains("@Total_Points_-_1st_Quarter"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time1);
                }

                else if (stakeValue.Contains("@Total_Points"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st);
                }

            }
        }

        public static void Tennis(IHtmlCollection<IElement> collection, Bet bet)
        {
            foreach (IElement element in collection)
            {
                var stakeValue = element.Attributes["data-selection-key"].Value ?? "";
                if (stakeValue == "")
                    continue;
                if (stakeValue.EndsWith(".yes")
                    || stakeValue.EndsWith(".no")
                    || stakeValue.EndsWith(".odd")
                    || stakeValue.EndsWith(".even")
                    || stakeValue.Contains("Asian_"))
                    continue;


                if (stakeValue.EndsWith("@Match_Result.1")
                    || stakeValue.EndsWith("@Match_Result0.1"))
                {
                    SetCoef(bet, BetNumber._1, element);
                }
                else if (stakeValue.EndsWith("@Match_Result.3")
                         || stakeValue.EndsWith("@Match_Result0.3"))
                {
                    SetCoef(bet, BetNumber._2, element);
                }

                //Первый тайм
                else if (stakeValue.EndsWith("@1st_Set_Result.RN_H")
                         || stakeValue.EndsWith("@1st_Set_Result0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@1st_Set_Result.RN_A")
                         || stakeValue.EndsWith("@1st_Set_Result0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time1);
                }

                //Второй тайм
                else if (stakeValue.EndsWith("@2nd_Set_Result.RN_H")
                         || stakeValue.EndsWith("@2nd_Set_Result0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@2nd_Set_Result.RN_A")
                         || stakeValue.EndsWith("@2nd_Set_Result0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time2);
                }


                //Третий тайм
                else if (stakeValue.EndsWith("@3rd_Set_Result.RN_H")
                         || stakeValue.EndsWith("@3rd_Set_Result0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time3);
                }

                else if (stakeValue.EndsWith("@3rd_Set_Result.RN_A")
                         || stakeValue.EndsWith("@3rd_Set_Result0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time3);
                }
                //TODO: Сделать 4й и 5й сеты. Форы/Тоталы/результат

                //Форы
                else if (stakeValue.Contains("@To_Win_Match_With_Handicap_By_Games"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st);
                }

                else if (stakeValue.Contains("@To_Win_1st_Set_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time1);
                }

                else if (stakeValue.Contains("@To_Win_2nd_Set_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time2);
                }

                else if (stakeValue.Contains("@To_Win_3rd_Set_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time3);
                }


                // ИТоталы
                else if (stakeValue.Contains("@First_Team_Total_Games"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st);
                }
                else if (stakeValue.Contains("@Second_Team_Total_Games"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st);
                }

                //Тоталы. Идем на оборот. Иначе могу не туда попасть
                else if (stakeValue.Contains("@3rd_Set_Total_Games"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time3);
                }

                else if (stakeValue.Contains("@2nd_Set_Total_Games"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time2);
                }

                else if (stakeValue.Contains("@1st_Set_Total_Games"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time1);
                }

                else if (stakeValue.Contains("@Total_Games"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st);
                }
                else if (stakeValue.Contains("@Set_{0}__game_{1}"))
                {

                    continue;
                    string data = element.ParentElement.ParentElement.ParentElement.Attributes["data-sel"].Value;
                    var json = JObject.Parse(data);
                    string[] gameData = json["mn"].ToString().Split(',');
                    var game =(TenisGamePart) Enum.Parse(typeof(TenisGamePart), gameData.Last().Split(' ').Last());
                    var part =(SportTimePart) Enum.Parse(typeof(SportTimePart), gameData.First().Split(' ').Last());

                    if (stakeValue.EndsWith(".RG_H"))
                    {
                        if (bet.Games.Count == 0)
                        {
                            GameBet gameBet = new GameBet();
                            gameBet.Team1 = bet.Team1;
                            gameBet.Team2 = bet.Team2;
                            gameBet.Set = part;

                        }
                        //1team
                    }
                    else if (stakeValue.EndsWith("RG_A"))
                    {
                        //2team
                    }
                }
            }
        }

        public static void Voleyball(IHtmlCollection<IElement> collection, Bet bet)
        {
            foreach (IElement element in collection)
            {
                var stakeValue = element.Attributes["data-selection-key"].Value ?? "";
                if (stakeValue == "")
                    continue;
                if (stakeValue.EndsWith(".yes")
                    || stakeValue.EndsWith(".no")
                    || stakeValue.EndsWith(".odd")
                    || stakeValue.EndsWith(".even")
                    || stakeValue.Contains("Asian_"))
                    continue;


                if (stakeValue.EndsWith("@To_Win_Match.1")
                    || stakeValue.EndsWith("@To_Win_Match0.1"))
                {
                    SetCoef(bet, BetNumber._1, element);
                }
                else if (stakeValue.EndsWith("@To_Win_Match.3")
                    || stakeValue.EndsWith("@To_Win_Match0.3"))
                {
                    SetCoef(bet, BetNumber._2, element);
                }

                //Первый тайм
                else if (stakeValue.EndsWith("@To_Win_1st_Set.RN_H")
                    || stakeValue.EndsWith("@To_Win_1st_Set0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@To_Win_1st_Set.RN_A")
                    || stakeValue.EndsWith("@To_Win_1st_Set0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time1);
                }

                //Второй тайм
                else if (stakeValue.EndsWith("@To_Win_2nd_Set.RN_H")
                    || stakeValue.EndsWith("@To_Win_2nd_Set0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@To_Win_2nd_Set.RN_A")
                    || stakeValue.EndsWith("@To_Win_2nd_Set0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time2);
                }


                //Третий тайм
                else if (stakeValue.EndsWith("@To_Win_3rd_Set.RN_H")
                    || stakeValue.EndsWith("@To_Win_3rd_Set0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time3);
                }

                else if (stakeValue.EndsWith("@To_Win_3rd_Set.RN_A")
                    || stakeValue.EndsWith("@To_Win_3rd_Set0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time3);
                }

                //Четвертая партия
                else if (stakeValue.EndsWith("@To_Win_4th_Set.RN_H")
                    || stakeValue.EndsWith("@To_Win_4th_Set0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time4);
                }

                else if (stakeValue.EndsWith("@To_Win_4th_Set.RN_A")
                    || stakeValue.EndsWith("@To_Win_4th_Set0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time4);
                }

                //Пятая партия
                else if (stakeValue.EndsWith("@To_Win_5th_Set.RN_H")
                    || stakeValue.EndsWith("@To_Win_5th_Set0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time5);
                }

                else if (stakeValue.EndsWith("@To_Win_5th_Set.RN_A")
                    || stakeValue.EndsWith("@To_Win_5th_Set0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time5);
                }

                //Форы
                else if (stakeValue.Contains("@Handicap_Betting_Points"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st);
                }

                else if (stakeValue.Contains("@1st_Set_Handicap_Points"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time1);
                }

                else if (stakeValue.Contains("@2nd_Set_Handicap_Points"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time2);
                }

                else if (stakeValue.Contains("@3rd_Set_Handicap_Points"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time3);
                }
                else if (stakeValue.Contains("@4th_Set_Handicap_Points"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time4);
                }
                else if (stakeValue.Contains("@5th_Set_Handicap_Points"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time5);
                }

                // ИТоталы
                else if (stakeValue.Contains("@Total_Points_(First_Team)_-_1st_Set"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st,SportTimePart.Time1);
                }
                else if (stakeValue.Contains("@Total_Points_(Second_Team)_-_1st_Set"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time1);
                }
                else if (stakeValue.Contains("@Total_Points_(First_Team)_-_2nd_Set"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time2);
                }
                else if (stakeValue.Contains("@Total_Points_(Second_Team)_-_2nd_Set"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time2);
                }
                else if (stakeValue.Contains("@Total_Points_(First_Team)_-_3rd_Set"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time3);
                }
                else if (stakeValue.Contains("@Total_Points_(Second_Team)_-_3rd_Set"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time3);
                }
                else if (stakeValue.Contains("@Total_Points_(First_Team)_-_4th_Set"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time4);
                }
                else if (stakeValue.Contains("@Total_Points_(Second_Team)_-_4th_Set"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time4);
                }

                else if (stakeValue.Contains("@Total_Points_(First_Team)_-_5th_Set"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time5);
                }
                else if (stakeValue.Contains("@Total_Points_(Second_Team)_-_5th_Set"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time5);
                }

                else if (stakeValue.Contains("@Total_Points_(First_Team)"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st);
                }
                else if (stakeValue.Contains("@Total_Points_(Second_Team)"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st);
                }



                //Тоталы. Идем на оборот. Иначе могу не туда попасть
                else if (stakeValue.Contains("@1st_Set_Total_Points"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time1);
                }

                else if (stakeValue.Contains("@2nd_Set_Total_Points"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time2);
                }

                else if (stakeValue.Contains("@3rd_Set_Total_Points"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time3);
                }
                else if (stakeValue.Contains("@4th_Set_Total_Points"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time4);
                }
                else if (stakeValue.Contains("@5th_Set_Total_Points"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time5);
                }
                else if (stakeValue.Contains("@Total_Points"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st);
                }

            }
        }

        public static void CommonTemp(IHtmlCollection<IElement> collection, Bet bet)
        {
            foreach (IElement element in collection)
            {
                var stakeValue = element.Attributes["data-selection-key"].Value ?? "";
                if (stakeValue == "")
                    continue;
                if (stakeValue.EndsWith(".yes")
                    || stakeValue.EndsWith(".no")
                    || stakeValue.EndsWith(".odd")
                    || stakeValue.EndsWith(".even")
                    || stakeValue.Contains("Asian_"))
                    continue;
                if (stakeValue.EndsWith("@Result.1")
                        || stakeValue.EndsWith("@Match_Winner_Including_All_OT.HB_H")
                        || stakeValue.EndsWith("@Match_Result.1")
                        || stakeValue.EndsWith("@To_Win_Match.1"))
                {
                    //Победа1
                    SetCoef(bet,BetNumber._1,element);
                }
                else if (stakeValue.EndsWith("@Result.draw"))
                {
                    //Ничья
                    SetCoef(bet, BetNumber._X, element);

                }
                else if (stakeValue.EndsWith("@Result.3")
                    || stakeValue.EndsWith("@Match_Winner_Including_All_OT.HB_A")
                    || stakeValue.EndsWith("@Match_Result.3")
                    || stakeValue.EndsWith("@To_Win_Match.3"))
                {
                    //Победа2
                    SetCoef(bet, BetNumber._2,element);

                }
                else if (stakeValue.EndsWith("@Result0.HD")
                    || stakeValue.EndsWith("@Double_Chance.HD"))
                {
                    //Победа1ИлиНичья
                    SetCoef(bet, BetNumber._1X, element);

                }
                else if (stakeValue.EndsWith("@Result0.HA")
                    || stakeValue.EndsWith("@Double_Chance.HA"))
                {
                    //Победа1илиПобеда2
                    SetCoef(bet, BetNumber._12, element);

                }
                else if (stakeValue.EndsWith("@Result0.AD")
                    || stakeValue.EndsWith("@Double_Chance.DA"))
                {
                    //Победа2ИлиНичья
                    SetCoef(bet, BetNumber._X2, element);
                }


            }
        }

        public static void Ganball(IHtmlCollection<IElement> collection, Bet bet)
        {
            foreach (IElement element in collection)
            {
                var stakeValue = element.Attributes["data-selection-key"].Value ?? "";
                if (stakeValue == "")
                    continue;
                if (stakeValue.EndsWith(".yes")
                    || stakeValue.EndsWith(".no")
                    || stakeValue.EndsWith(".odd")
                    || stakeValue.EndsWith(".even")
                    || stakeValue.Contains("Asian_"))
                    continue;


                if (stakeValue.EndsWith("@Result.1")
                    || stakeValue.EndsWith("@Result0.1"))
                {
                    SetCoef(bet, BetNumber._1, element);
                }
                if (stakeValue.EndsWith("@Result.draw")
                    || stakeValue.EndsWith("@Result0.draw"))
                {
                    SetCoef(bet, BetNumber._X, element);
                }
                else if (stakeValue.EndsWith("@Result.3")
                    || stakeValue.EndsWith("@Result0.3"))
                {
                    SetCoef(bet, BetNumber._2, element);
                }

                if (stakeValue.EndsWith("@Double_Chance.HD")
                    || stakeValue.EndsWith("@Double_Chance0.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element);
                }
                if (stakeValue.EndsWith("@Double_Chance.HA")
                    || stakeValue.EndsWith("@Double_Chance0.HA"))
                {
                    SetCoef(bet, BetNumber._12, element);
                }
                else if (stakeValue.EndsWith("@Double_Chance.DA")
                    || stakeValue.EndsWith("@Double_Chance0.DA"))
                {
                    SetCoef(bet, BetNumber._X2, element);
                }
                //Первый тайм
                else if (stakeValue.EndsWith("@1st_Half_Result.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@1st_Half_Result.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@1st_Half_Result.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@1st_Half_Double_Chance.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@1st_Half_Double_Chance.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@1st_Half_Double_Chance.DA"))
                {
                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Time1);
                }

                //Второй тайм
                else if (stakeValue.EndsWith("@2nd_Half_Result.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@2nd_Half_Result.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@2nd_Half_Result.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@2nd_Half_Double_Chance.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@2nd_Half_Double_Chance.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@2nd_Half_Double_Chance.DA"))
                {
                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Time2);
                }

                //Форы
                else if (stakeValue.Contains("@Handicap_Betting")
                    || stakeValue.Contains("@Money_Line"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st);
                }

                else if (stakeValue.Contains("@First_Half_Handicap_Betting_"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time1);
                }

                else if (stakeValue.Contains("@Second_Half_Handicap_Betting_"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time2);
                }


                else if (stakeValue.Contains("@First_Team_Total_Goals"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st);
                }
                else if (stakeValue.Contains("@Second_Team_Total_Goals"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st);
                }

                //Тоталы. 
                else if (stakeValue.Contains("@First_Half_Goals"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time1);
                }

                else if (stakeValue.Contains("@Second_Half_Goals"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time2);
                }

                else if (stakeValue.Contains("@Total_Goals"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st);
                }

            }
        }

        public static void Polo(IHtmlCollection<IElement> collection, Bet bet)
        {
            foreach (IElement element in collection)
            {
                var stakeValue = element.Attributes["data-selection-key"].Value ?? "";
                if (stakeValue == "")
                    continue;
                if (stakeValue.EndsWith(".yes")
                    || stakeValue.EndsWith(".no")
                    || stakeValue.EndsWith(".odd")
                    || stakeValue.EndsWith(".even")
                    || stakeValue.Contains("Asian_"))
                    continue;


                if (stakeValue.EndsWith("@Result.1")
                    || stakeValue.EndsWith("@Result0.1"))
                {
                    SetCoef(bet, BetNumber._1, element);
                }
                if (stakeValue.EndsWith("@Result.draw")
                    || stakeValue.EndsWith("@Result0.draw"))
                {
                    SetCoef(bet, BetNumber._X, element);
                }
                else if (stakeValue.EndsWith("@Result.3")
                    || stakeValue.EndsWith("@Result0.3"))
                {
                    SetCoef(bet, BetNumber._2, element);
                }

                if (stakeValue.EndsWith("@Double_Chance.HD")
                    || stakeValue.EndsWith("@Double_Chance0.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element);
                }
                if (stakeValue.EndsWith("@Double_Chance.HA")
                    || stakeValue.EndsWith("@Double_Chance0.HA"))
                {
                    SetCoef(bet, BetNumber._12, element);
                }
                else if (stakeValue.EndsWith("@Double_Chance.DA")
                    || stakeValue.EndsWith("@Double_Chance0.DA"))
                {
                    SetCoef(bet, BetNumber._X2, element);
                }
                //Первый тайм
                else if (stakeValue.EndsWith("@Result_-_1st_Period.RN_H")
                    || stakeValue.EndsWith("@Result_-_1st_Period0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@Result_-_1st_Period.RN_D")
                    || stakeValue.EndsWith("@Result_-_1st_Period0.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@Result_-_1st_Period.RN_A")
                    || stakeValue.EndsWith("@Result_-_1st_Period0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@Double_Chance_-_1st_Period.HD")
                    || stakeValue.EndsWith("@Double_Chance_-_1st_Period0.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@Double_Chance_-_1st_Period.HA")
                    || stakeValue.EndsWith("@Double_Chance_-_1st_Period0.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Time1);
                }

                else if (stakeValue.EndsWith("@Double_Chance_-_1st_Period.DA")
                    || stakeValue.EndsWith("@Double_Chance_-_1st_Period0.DA"))
                {
                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Time1);
                }

                //Второй тайм
                else if (stakeValue.EndsWith("@Result_-_2nd_Period.RN_H")
                    || stakeValue.EndsWith("@Result_-_2nd_Period0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@Result_-_2nd_Period.RN_D")
                    || stakeValue.EndsWith("@Result_-_2nd_Period0.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@Result_-_2nd_Period.RN_A")
                    || stakeValue.EndsWith("@Result_-_2nd_Period0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@Double_Chance_-_2nd_Period.HD")
                    || stakeValue.EndsWith("@Double_Chance_-_2nd_Period0.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@Double_Chance_-_2nd_Period.HA")
                    || stakeValue.EndsWith("@Double_Chance_-_2nd_Period0.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Time2);
                }

                else if (stakeValue.EndsWith("@Double_Chance_-_2nd_Period.DA")
                    || stakeValue.EndsWith("@Double_Chance_-_2nd_Period0.DA"))
                {
                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Time2);
                }

                //Третий тайм
                else if (stakeValue.EndsWith("@Result_-_3rd_Period.RN_H")
                    || stakeValue.EndsWith("@Result_-_3rd_Period0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time3);
                }

                else if (stakeValue.EndsWith("@Result_-_3rd_Period.RN_D")
                    || stakeValue.EndsWith("@Result_-_3rd_Period0.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Time3);
                }

                else if (stakeValue.EndsWith("@Result_-_3rd_Period.RN_A")
                    || stakeValue.EndsWith("@Result_-_3rd_Period0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time3);
                }

                else if (stakeValue.EndsWith("@Double_Chance_-_3rd_Period.HD")
                    || stakeValue.EndsWith("@Double_Chance_-_3rd_Period0.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Time3);
                }

                else if (stakeValue.EndsWith("@Double_Chance_-_3rd_Period.HA")
                    || stakeValue.EndsWith("@Double_Chance_-_3rd_Period0.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Time3);
                }

                else if (stakeValue.EndsWith("@Double_Chance_-_3rd_Period.DA")
                    || stakeValue.EndsWith("@Double_Chance_-_3rd_Period0.DA"))
                {

                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Time3);
                }

                //Четвертый тайм
                else if (stakeValue.EndsWith("@Result_-_4th_Period.RN_H")
                    || stakeValue.EndsWith("@Result_-_4th_Period0.RN_H"))
                {
                    SetCoef(bet, BetNumber._1, element, SportTimePart.Time4);
                }

                else if (stakeValue.EndsWith("@Result_-_4th_Period.RN_D")
                    || stakeValue.EndsWith("@Result_-_4th_Period0.RN_D"))
                {
                    SetCoef(bet, BetNumber._X, element, SportTimePart.Time4);
                }

                else if (stakeValue.EndsWith("@Result_-_4th_Period.RN_A")
                    || stakeValue.EndsWith("@Result_-_4th_Period0.RN_A"))
                {
                    SetCoef(bet, BetNumber._2, element, SportTimePart.Time4);
                }

                else if (stakeValue.EndsWith("@Double_Chance_-_4th_Period.HD")
                    || stakeValue.EndsWith("@Double_Chance_-_4th_Period0.HD"))
                {
                    SetCoef(bet, BetNumber._1X, element, SportTimePart.Time4);
                }

                else if (stakeValue.EndsWith("@Double_Chance_-_4th_Period.HA")
                    || stakeValue.EndsWith("@Double_Chance_-_4th_Period0.HA"))
                {
                    SetCoef(bet, BetNumber._12, element, SportTimePart.Time4);
                }

                else if (stakeValue.EndsWith("@Double_Chance_-_4th_Period.DA")
                    || stakeValue.EndsWith("@Double_Chance_-_4th_Period0.DA"))
                {
                    SetCoef(bet, BetNumber._X2, element, SportTimePart.Time4);
                }

                //Форы
                else if (stakeValue.Contains("@Result_With_Handicap_-_1st_Period"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time1);
                }

                else if (stakeValue.Contains("@Result_With_Handicap_-_2nd_Period"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time2);
                }
                else if (stakeValue.Contains("@Result_With_Handicap_-_3rd_Period"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time3);
                }
                else if (stakeValue.Contains("@Result_With_Handicap_-_4th_Period"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time4);
                }
                else if (stakeValue.Contains("@Result_With_Handicap"))
                {
                    Stake st = new Stake();
                    SetForaValues(element, stakeValue, st);
                    AddStake(bet, st);
                }

                //Ит
                else if (stakeValue.Contains("@Total_Goals_(First_Team)_-_1st_Period"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time1);
                }
                else if (stakeValue.Contains("@Total_Goals_(Second_Team)_-_1st_Period"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time1);

                }
                else if (stakeValue.Contains("@Total_Goals_(First_Team)_-_2nd_Period"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time2);
                }
                else if (stakeValue.Contains("@Total_Goals_(Second_Team)_-_2nd_Period"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time2);
                }
                else if (stakeValue.Contains("@Total_Goals_(First_Team)_-_3rd_Period"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time3);
                }
                else if (stakeValue.Contains("@Total_Goals_(Second_Team)_-_3rd_Period"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time3);
                }
                else if (stakeValue.Contains("@Total_Goals_(First_Team)_-_4th_Period"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time4);
                }
                else if (stakeValue.Contains("@Total_Goals_(Second_Team)_-_4th_Period"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st, SportTimePart.Time4);
                }
                else if (stakeValue.Contains("@Total_Goals_(First_Team)"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team1;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st);
                }
                else if (stakeValue.Contains("@Total_Goals_(Second_Team)"))
                {
                    Stake st = new Stake();
                    st.Team = ETeam.Team2;
                    SetTotalValues(element, stakeValue, st, true);
                    AddStake(bet, st);
                }

                //Тоталы. 
                else if (stakeValue.Contains("@Total_Goals_-_1st_Period"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time1);
                }

                else if (stakeValue.Contains("@Total_Goals_-_2nd_Period"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time2);
                }
                else if (stakeValue.Contains("@Total_Goals_-_3rd_Period"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time3);
                }

                else if (stakeValue.Contains("@Total_Goals_-_4th_Period"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st, SportTimePart.Time4);
                }

                else if (stakeValue.Contains("@Total_Goals"))
                {
                    Stake st = new Stake();
                    SetTotalValues(element, stakeValue, st);
                    AddStake(bet, st);
                }

            }
        }


        /// <summary>
        /// Задает необходимые значение ставке Тотал и Ит
        /// </summary>
        /// <param name="element"></param>
        /// <param name="stakeValue"></param>
        /// <param name="st"></param>
        /// <param name="iTotal"></param>
        private static void SetTotalValues(IElement element, string stakeValue, Stake st,bool iTotal=false)
        {
            if (stakeValue.Contains("Under"))
            {
                if(iTotal)
                    st.StakeType = StakeType.ITmin;
                else
                    st.StakeType = StakeType.Tmin;
            }
            else if (stakeValue.Contains("Over"))
            {
                if (iTotal)
                    st.StakeType = StakeType.ITmax;
                else
                    st.StakeType = StakeType.Tmax;
            }
            st.ParametrO = stakeValue;
            st.Parametr = GetValue(stakeValue.Split('_').Last());
            st.Coef = GetCorectValue(element);
        }

        /// <summary>
        /// Задает необходимые значение ставке Фора
        /// </summary>
        /// <param name="element"></param>
        /// <param name="stakeValue"></param>
        /// <param name="st"></param>
        private static void SetForaValues(IElement element, string stakeValue, Stake st)
        {
            if (stakeValue.EndsWith(".HB_H"))
                st.StakeType = StakeType.Fora1;
            else
            {
                st.StakeType = StakeType.Fora2;
            }

            st.Coef = GetCorectValue(element);
            st.ParametrO = stakeValue;
            string parametr = element.Parent.ChildNodes[0].TextContent.Trim();
            if (String.IsNullOrWhiteSpace(parametr))
                parametr = element.Parent.Parent.ChildNodes[1].TextContent.Trim();
            st.Parametr = GetValue(parametr);
        }

        /// <summary>
        /// Устанавливает коэфицент заддоной ставки в событие
        /// </summary>
        /// <param name="mainBet"></param>
        /// <param name="betType"></param>
        /// <param name="node"></param>
        /// <param name="time"></param>
        private static void SetCoef(Bet mainBet, BetNumber betType, IElement node,SportTimePart time= SportTimePart.Match)
        {
            Bet bet = GetOrCreateTimePart(mainBet, time);

            bet[betType] = GetCorectValue(node);
            if (bet[betType] != 0)
            {
                bet.SetData(betType, node.Attributes["data-selection-key"].Value);
            }
        }

        /// <summary>
        /// Добавляет в собтие нужную часть ставки
        /// </summary>
        /// <param name="mainBet"></param>
        /// <param name="stake"></param>
        /// <param name="time"></param>
        public static void AddStake(Bet mainBet, Stake stake, SportTimePart time = SportTimePart.Match)
        {
            Bet bet = GetOrCreateTimePart(mainBet, time); 
            if(stake.StakeType==StakeType.Fora1
                || stake.StakeType == StakeType.Fora2)
                bet.Foras.Add(stake);
            else if(stake.StakeType==StakeType.Tmax
                ||stake.StakeType==StakeType.Tmin)
                bet.Totals.Add(stake);
            else if(stake.StakeType==StakeType.ITmax
                || stake.StakeType==StakeType.ITmin)
                bet.ITotals.Add(stake);
        }


        /// <summary>
        /// Возвращает нужную часть события, в зависимости от тайма( Если нужной части нет, то создает)
        /// </summary>
        /// <param name="mainBet"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        private static Bet GetOrCreateTimePart(Bet mainBet, SportTimePart time)
        {
            Bet bet;
            if (time == SportTimePart.Match)
                bet = mainBet;
            else
            {
                if (mainBet.Parts.ContainsKey(time))
                {
                    bet = mainBet.Parts[time];
                }
                else
                {
                    bet = mainBet.ShortCopy();
                    mainBet.Parts.Add(time, bet);
                }
            }

            return bet;
        }



        /// <summary>
        /// Берет верное значение коэф из span`a
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static float GetCorectValue(IElement node)
        {
            if (node.Attributes["data-selection-price"] == null)
                return 0;
            string temp = node.Attributes["data-selection-price"].Value.Trim().Replace(".", ",");
            return Convert.ToSingle(temp);
        }

        /// <summary>
        /// Задает правильный значение параметра для форы/тотала
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static float GetValue(string text)
        {
            text = text.Replace("(", "").Replace(")", "").Trim().Replace(".", ",");
            return Convert.ToSingle(text);
        }
    }
}
