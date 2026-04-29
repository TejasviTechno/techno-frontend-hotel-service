namespace XConnect.Supplier.Exely
{
    public class ExelyInfo
    {
        public string BaseUrl { get; set; }
        public string SearchRoomStays { get; set; } = string.Empty;
        public string VerifyBooking { get; set; }
        public string CreateBooking { get; set; }
        public string GetBooking { get; set; }
        public string CalculateCancellationPenalty { get; set; }
        public string CancelBooking { get; set; }
        public string GetCancellationRulesByProperty { get; set; }
        public string GetRoomStaysByProperty { get; set; }
        public string? PropertyContent { get; set; }
    }
}
