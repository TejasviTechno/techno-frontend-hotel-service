using System.Data;


namespace XConnect.Models.Hotel
{
    public class HotelRepository
    {
        
    }

    public class HotelX
    {
        public bool IsSeperate { get; set; } = false;
        public List<RQRS> RQRS { get; set; }
        public string Supplier { get; set; }
        public bool DebugeMode { get; set; }
        public string URL { get; set; }
        public int SearchTimeOut { get; set; }
        public int ProcessTimeout { get; set; }
        public string Action { get; set; }
        public double CurrencyFactor { get; set; }
        public string CurrencyCode { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string PassWord { get; set; }

        public string MethoodType { get; set; }
        public DataTable dtHotel { get; set; }
        public string Version { get; set; }
        public string EchoToken { get; set; }
        public string PlatForm { get; set; }
        public string XmlCity { get; set; }
        public string XmlCountry { get; set; }
        public string XmlNationality { get; set; }


        public string ownsystemhotelId { get; set; }
        public string xmlhotelcode { get; set; }
        public int hotelRating { get; set; }
        public string GIATACode { get; set; }
        public string AvailableToken { get; set; }
        public string CID { get; set; }
        public string Language { get; set; }
        public DataSet dsTemp { get; set; }
        public string NewPara { get; set; }
        public HotelSearchParameter SearchPara { get; set; }
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        public int CityId { get; set; }
        public DataRow[] drhotel { get; set; }
        public bool IsConvert { get; set; }
        public int StarRating { get; set; }
        public string StrHotelIds { get; set; }
        public List<Options> dtHotelRoom { get; set; }

        public int AgentID { get; set; }
        public string ReportType { get; set; }
        public string ResponseType { get; set; }
        public string RSTimeout { get; set; }
        public int ResponseStatus { get; set; }

        public double AgentWiseMarkup { get; set; }
        public int SupplierID { get; set; }
        public Boolean IsShowSupplier { get; set; }
        public int EanProfit { get; set; }
        public int ReturnRooms { get; set; } //210225
        public string TechName { get; set; } //210225

        public int MinRoomCount { get; set; } //for rategain 
        public int MaxRoomCount { get; set; } //for rategain 
        public double Commission { get; set; } //for booking.com
        public int MasterSupplierID { get; set; } //for booking.com
        public string  SupplierCurrency { get; set; } //for hoteldo

        public string ResponseError { get; set; } //190625
        public int Host { get; set; } //17-DEC-2025
        public string ActualReason { get; set; } // added 20042026
        public double CodeProcessingTimeSupplier { get; set; } // added 30042026
        
        public string ListHotelId { get; set; } //190625
        public string SupplierResponseError { get; set; } //added 05052026
    }

    public class RQRS
    {
        public string Request { get; set; }
        public string Response { get; set; }
    }
}
