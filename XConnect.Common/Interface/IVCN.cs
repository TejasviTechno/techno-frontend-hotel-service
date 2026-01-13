using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.CommonUtilities.Interface
{
    public interface IVCN
    {
        void GetApisoServiceDatatoCreateVCN(string ReferenceNo, int BookingId, int BookingDetailId, ref string Pan, ref string CVV, ref string Expiry, bool IsPruvo);

        void GetApisoServiceDatatoReloadVCNDebitNonconfirmedbookings(string ReferenceNo, int BookingId, int BookingDetailId);

        void updateCardOldData_ReBooking(string referenceNo, int bookingId, int detailId);

        void updateCardOldData_Fail_ReBooking(string referenceNo, int bookingId, int detailId);
    }
}
