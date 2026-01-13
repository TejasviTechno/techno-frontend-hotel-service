
using XConnect.Models.Hotel;

namespace Xconnect.API.Extensions
{
    public class HotelXML_Extension
    {
        public static bool CountryBlock(Int32 CountryId)
        {
            try
            {
                bool IsBlock = true;
                switch (CountryId)// Only get response in Saudi Arabia Ticket#12938
                {
                    case 15471: { IsBlock = false; } break;

                    default:
                        return true;
                }
                return IsBlock;
            }
            catch (Exception ex)
            {
                //Common.Error(ex);
                return true;
            }
        }
    }

    public static class Extensions
    {
        public static string DateValidate(this string date, string format)
        {
            DateTime result;
            try
            {
                result = DateTime.ParseExact(date, format, cultureInfo);
                return "";
            }
            catch (FormatException)
            {
                return string.Format("{0} is not in the correct format (MM-dd-yyyy).", date);
            }
        }
        public static string SearchError(string Parameter, string Type)
        {
            return string.Format("{0} must {1}.", Parameter, Type);
        }

        public static string RequiredParameter = " required";
        public static string BetweenParameter = " between";
        public static string GreaterThan = " greater than";
        //public static string DateFormateRegex = "^([0]?[0-9]|[12][0-9]|[3][01])[.--]([0]?[1-9]|[1][0-2])[.--]([0-9]{4}|[0-9]{2})$";
        public static string MM_DD_YYYY = "MM-dd-yyyy";
        public static System.Globalization.CultureInfo cultureInfo = System.Globalization.CultureInfo.InvariantCulture;
        //public static string AgentWiseValidation(this HotelSearchParameter RQ, Int32 AgentId)
        //{
        //    try
        //    {
        //        if (AgentId == 4440)
        //        {
        //            bool IsCityBlock = HotelXML_Extension.CountryBlock(RQ.CountryID);
        //            if (IsCityBlock == true)
        //            {
        //                return "Requested City is not allow for this account !!";
        //            }
        //        }
        //        return "";
        //    }
        //    catch (Exception ex)
        //    {
        //        //Common.Error(ex);
        //        return "";
        //    }
        //}

        public static string ValidateAvailabilityRQ(this HotelSearchParameter RQ)
        {
            try
            {
                //if (RQ.CityID == 0)
                //    return SearchError("CityID", RequiredParameter);
                if (RQ.NoofNights == 0 || RQ.NoofNights > 30)
                    return SearchError("NoofNights", BetweenParameter + " 1 and 30");
                var days = (Convert.ToDateTime(RQ.CheckOutDate) - Convert.ToDateTime(RQ.CheckInDate)).TotalDays;

                if (RQ.NoofNights != Convert.ToInt32(days))
                    return SearchError("NoofNights", "Not correct");
                if (RQ.CheckInDate == "" || RQ.CheckOutDate == "")
                    return SearchError("Traveldates", RequiredParameter);
                if (RQ.Nationality == "")
                    return SearchError("Nationality", RequiredParameter);
                if (RQ.Nationality == "")
                    return SearchError("Nationality", RequiredParameter);
                if (RQ.Rooms.Count == 0)
                    return SearchError("Rooms", RequiredParameter);
                if (RQ.Rooms.Count > 9)
                    return SearchError("Rooms", BetweenParameter + " 1 and 9");
                if (RQ.HotelID == "" && (RQ.Filters.StarRating.Min > 5 || RQ.Filters.StarRating.Max > 5))
                    return SearchError("StarRating", BetweenParameter + " 0 and 5");
                if (RQ.HotelID == "" && (RQ.Filters.StarRating.Min > RQ.Filters.StarRating.Max))
                    return SearchError("StarRating.Max", GreaterThan + " StarRating.Min");

                string dateval = "";
                dateval = RQ.CheckInDate.DateValidate(MM_DD_YYYY);
                if (dateval != "")
                    return dateval;
                dateval = RQ.CheckOutDate.DateValidate(MM_DD_YYYY);
                if (dateval != "")
                    return dateval;
                //  var days = (Convert.ToDateTime(RQ.CheckOutDate) - Convert.ToDateTime(RQ.CheckInDate)).TotalDays;
                if (days > 30)
                    return SearchError("Traveldates", RequiredParameter);

                bool PaxDetailError = false;
                List<int> MainList = new List<int>();
                List<int> roomList = new List<int>();

                for (int i = 0; i < RQ.Rooms.Count; i++)
                {
                    roomList.Add(RQ.Rooms[i].RoomNo);
                    MainList.Add(i + 1);
                    if (RQ.Rooms[i].NoofAdults == 0)
                    {
                        PaxDetailError = true;
                        break;
                    }
                    if (RQ.Rooms[i].NoofChild > 0)
                    {
                        if (RQ.Rooms[i].NoofChild > 2)
                        {
                            PaxDetailError = true;
                            break;
                        }

                        if ((RQ.Rooms[i].NoofChild == 1 && RQ.Rooms[i].Child1Age == 0) || (RQ.Rooms[i].NoofChild == 2 && (RQ.Rooms[i].Child1Age == 0 || RQ.Rooms[i].Child2Age == 0)))
                        {
                            PaxDetailError = true;
                            break;
                        }
                        if (RQ.Rooms[i].Child1Age > 12 || RQ.Rooms[i].Child2Age > 12)
                        {
                            return SearchError("Child Age ", BetweenParameter + " 1 and 12");
                        }
                    }
                }
                if (PaxDetailError)
                    return "please try to pass pax details correctly";
                for (int i = 0; i < MainList.Count; i++)
                {
                    if (MainList[i] != roomList[i])
                    {
                        PaxDetailError = true;
                        break;
                    }
                }
                if (PaxDetailError)
                    return "please try to pass pax details correctly";
                return "";
            }
            catch (Exception ex)
            {
                //Common.Error(ex);
                return "";
            }
        }
    }

}
