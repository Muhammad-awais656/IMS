using System.ComponentModel.DataAnnotations;

namespace IMS.Common_ViewModels
{
    public class UserViewModel
    {
        public int DomainId { get; set; }
        public int RoleId { get; set; }
        public string UserPassword { get; set; }
        public int id { get; set; }
        public int[] SocitiesIds { get; set; }
        public int SubDomainId { get; set; }
        public bool Finance { get; set; }
        public string SubDomainName { get; set; }
        public string FinanceUser { get; set; }
        public string DomainName { get; set; }
        public int PostId { get; set; }
        public string PostName { get; set; }


        public string PhaseExtension { get; set; }
        public int ProvinceId { get; set; }
        public int DivisionId { get; set; }
        public string DivisionName { get; set; }
        public int DistrictId { get; set; }
        public string DistrictName { get; set; }
        public int TehsilId { get; set; }
        public string TehsilName { get; set; }
        public int OfficerId { get; set; }
        public string OfficerName { get; set; }
        public int UserOId { get; set; }

        [MaxLength(256)]
        public string UserName { get; set; }
        public string Password { get; set; }
        public string NewPassword { get; set; }
        public string OldPassword { get; set; }
        public string User_Id { get; set; }
        public string CNIC { get; set; }
        public string Previllage_Id { get; set; }
        public string PhoneNumber { get; set; }
        public string OfficerCNIC { get; set; }
        public string ADUserName { get; set; }
        public List<UserBranchesViewModel> Branches { get; set; }
        public string? BIName { get; set; }
        public string? BIContactNo { get; set; }
        public string Email { get; internal set; }
    }
    public class UserBranchesViewModel
    {
        public int BranchId { get; set; }

        public string? BranchCode { get; set; }

        public string? BranchName { get; set; }

        public string? Address { get; set; }

        public string? Phone { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? CreatedDate { get; set; }

        public long? CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public long? ModifiedBy { get; set; }

    }

}
