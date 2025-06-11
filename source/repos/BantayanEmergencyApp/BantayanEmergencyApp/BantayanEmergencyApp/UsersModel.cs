using System;

namespace BantayanEmergencyApp
{
    public class UsersModel
    {
        public string Key { get; set; }  // 🔥 Unique Key from Firebase (This is needed)
        public string UserId { get; set; } // Ensure this exists
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public string UserType { get; set; }
        public string ValidID { get; set; }
        public string FacePicture { get; set; }
        public string Status { get; set; }
        public string RejectionReason { get; set; }
        public string FcmToken { get; set; }
        public string ValidIDBack { get; set; }
        public bool IsExpanded { get; set; } = false;
        public DateTime? BanEndDate { get; set; }
        public string RemainingTime { get; set; }
        public bool IsPermanentlyBanned => !BanEndDate.HasValue;
        public bool IsDeleted { get; set; } = false;
        public string DeviceToken { get; set; }
        public string CertificateImage { get; set; }
        public object BanReason { get; set; }
        public object Timestamp { get; set; }
        public string DateApproved { get; set; }
        public string PendingContactNumber { get; set; }


    }

}
