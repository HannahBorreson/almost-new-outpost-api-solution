using AlmostNewOutpost.Api.Services;

namespace AlmostNewOutpost.Tests.Unit;

/// <summary>
/// The full role-to-category matrix, plus unknown roles and categories.
/// </summary>
public class CategoryAccessPolicyTests
{
    [Theory]
    [InlineData("Citizen", "Basic", true)]
    [InlineData("Citizen", "Exploration", false)]
    [InlineData("Citizen", "Medical", false)]
    [InlineData("Citizen", "Security", false)]
    [InlineData("Scout", "Basic", true)]
    [InlineData("Scout", "Exploration", true)]
    [InlineData("Scout", "Medical", false)]
    [InlineData("Scout", "Security", false)]
    [InlineData("Medic", "Basic", true)]
    [InlineData("Medic", "Exploration", false)]
    [InlineData("Medic", "Medical", true)]
    [InlineData("Medic", "Security", false)]
    [InlineData("Security", "Basic", true)]
    [InlineData("Security", "Exploration", false)]
    [InlineData("Security", "Medical", false)]
    [InlineData("Security", "Security", true)]
    [InlineData("MerchantAdmin", "Basic", true)]
    [InlineData("MerchantAdmin", "Exploration", true)]
    [InlineData("MerchantAdmin", "Medical", true)]
    [InlineData("MerchantAdmin", "Security", true)]
    public void CanBuy_FollowsTheMatrix(string role, string category, bool expected)
    {
        Assert.Equal(expected, CategoryAccessPolicy.CanBuy(role, category));
    }

    [Theory]
    [InlineData("Trader")]
    [InlineData("")]
    [InlineData(null)]
    public void UnknownRole_CannotBuyAnything(string? role)
    {
        Assert.False(CategoryAccessPolicy.CanBuy(role, "Basic"));
    }

    [Theory]
    [InlineData("Weapons")]
    [InlineData("")]
    [InlineData(null)]
    public void UnknownCategory_CannotBeBought(string? category)
    {
        Assert.False(CategoryAccessPolicy.CanBuy("MerchantAdmin", category));
    }
}
