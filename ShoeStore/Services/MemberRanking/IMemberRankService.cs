using ShoeStore.Models;

namespace ShoeStore.Services.MemberRanking
{
    public interface IMemberRankService
    {
        MemberRank GetMemberRankBySpentAmount(decimal totalSpent);
        Task<MemberRank> GetMemberRankById(int id);
        Task<List<MemberRank>> GetAllMemberRanks();
        decimal CalculateDiscountAmount(decimal originalPrice, MemberRank rank);
        Task UpdateUserRank(int userId);
    }
}