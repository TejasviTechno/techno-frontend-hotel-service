
using XConnect.Models;

namespace XConnect.ErrorLogHandler.Interface
{
    public interface IException
    {
        Task Error(Exception ex, string SearchKey = "", string Info = "");
        Task ErrorBooking(Exception ex, string AgentId, string ErrorNo = "", string SearchKey = "");

        Task getException_v1(string ex, string file_name, string title, string provider, string SearchKey = "", string Info = "");
        Task getException_v2(string ex, string file_name, string title, string provider, string SearchKey = "", string Info = "");
        void ErrorLog(string sPathName, string sErrMsg);
        Task ErrorLogAsync(string sPathName, string sErrMsg);

        Task ErrorNew(Exception ex, ErrorLog._LogAPI api, string SearchKey = "", string Info = "");

        Task getException(Exception ex, string file_name, string title, string provider, string SearchKey = "", string Info = "");

    }
}
