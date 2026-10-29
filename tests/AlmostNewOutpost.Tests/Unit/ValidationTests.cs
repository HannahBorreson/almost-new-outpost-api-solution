using System.ComponentModel.DataAnnotations;
using AlmostNewOutpost.Api.Models.Requests;
using AlmostNewOutpost.Api.Weather;

namespace AlmostNewOutpost.Tests.Unit;

/// <summary>
/// Each request model run through the same DataAnnotations checks [ApiController] uses,
/// including the values right on each boundary.
/// </summary>
public class ValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        List<ValidationResult> results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static bool HasErrorFor(List<ValidationResult> results, string member)
    {
        return results.Any(result => result.MemberNames.Contains(member));
    }

    // ---- LoginRequest ----

    [Theory]
    [InlineData(1, 1)]
    [InlineData(50, 128)]
    public void Login_ValidLengths_Pass(int usernameLength, int passwordLength)
    {
        LoginRequest request = new LoginRequest
        {
            Username = new string('u', usernameLength),
            Password = new string('p', passwordLength),
        };

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData("", "secret", "Username")]
    [InlineData("   ", "secret", "Username")]
    [InlineData("medic", "", "Password")]
    public void Login_BlankFields_Fail(string username, string password, string failingField)
    {
        LoginRequest request = new LoginRequest { Username = username, Password = password };

        Assert.True(HasErrorFor(Validate(request), failingField));
    }

    [Fact]
    public void Login_TooLong_Fails()
    {
        LoginRequest request = new LoginRequest { Username = new string('u', 51), Password = new string('p', 129) };
        List<ValidationResult> results = Validate(request);

        Assert.True(HasErrorFor(results, "Username"));
        Assert.True(HasErrorFor(results, "Password"));
    }

    // ---- CreatePurchaseRequest ----

    [Fact]
    public void Purchase_ProductIdOne_Passes()
    {
        Assert.Empty(Validate(new CreatePurchaseRequest { ProductId = 1 }));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public void Purchase_MissingOrTooSmallProductId_Fails(int? productId)
    {
        Assert.True(HasErrorFor(Validate(new CreatePurchaseRequest { ProductId = productId }), "ProductId"));
    }

    // ---- ProductUpsertRequest ----

    private static ProductUpsertRequest ValidProduct()
    {
        return new ProductUpsertRequest
        {
            Name = "Seed Packet",
            Description = "Heirloom tomato seeds.",
            CategoryId = 1,
            PriceCredits = 8,
            IconKey = "ration",
        };
    }

    [Fact]
    public void Product_ValidRequest_Passes()
    {
        Assert.Empty(Validate(ValidProduct()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100000)]
    public void Product_PriceOnTheBoundary_Passes(int price)
    {
        ProductUpsertRequest request = ValidProduct();
        request.PriceCredits = price;

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(100001)]
    public void Product_PriceOutOfRange_Fails(int? price)
    {
        ProductUpsertRequest request = ValidProduct();
        request.PriceCredits = price;

        Assert.True(HasErrorFor(Validate(request), "PriceCredits"));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("  ab  ")]
    public void Product_NameOfTwoCharactersAfterTrimming_Passes(string name)
    {
        ProductUpsertRequest request = ValidProduct();
        request.Name = name;

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("  a  ")]
    public void Product_NameTooShort_Fails(string name)
    {
        ProductUpsertRequest request = ValidProduct();
        request.Name = name;

        Assert.True(HasErrorFor(Validate(request), "Name"));
    }

    [Fact]
    public void Product_NameOf100Characters_Passes_And101_Fails()
    {
        ProductUpsertRequest ok = ValidProduct();
        ok.Name = new string('n', 100);
        ProductUpsertRequest tooLong = ValidProduct();
        tooLong.Name = new string('n', 101);

        Assert.Empty(Validate(ok));
        Assert.True(HasErrorFor(Validate(tooLong), "Name"));
    }

    [Fact]
    public void Product_DescriptionOf500Characters_Passes_And501_Fails()
    {
        ProductUpsertRequest ok = ValidProduct();
        ok.Description = new string('d', 500);
        ProductUpsertRequest tooLong = ValidProduct();
        tooLong.Description = new string('d', 501);

        Assert.Empty(Validate(ok));
        Assert.True(HasErrorFor(Validate(tooLong), "Description"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void Product_MissingCategory_Fails(int? categoryId)
    {
        ProductUpsertRequest request = ValidProduct();
        request.CategoryId = categoryId;

        Assert.True(HasErrorFor(Validate(request), "CategoryId"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("first-aid")]
    [InlineData("Icon2")]
    public void Product_IconKey_AllowedValues_Pass(string? iconKey)
    {
        ProductUpsertRequest request = ValidProduct();
        request.IconKey = iconKey;

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData("first aid")]
    [InlineData("icon_key")]
    [InlineData("<script>")]
    public void Product_IconKey_WithOtherCharacters_Fails(string iconKey)
    {
        ProductUpsertRequest request = ValidProduct();
        request.IconKey = iconKey;

        Assert.True(HasErrorFor(Validate(request), "IconKey"));
    }

    [Fact]
    public void Product_IconKeyOf51Characters_Fails()
    {
        ProductUpsertRequest request = ValidProduct();
        request.IconKey = new string('k', 51);

        Assert.True(HasErrorFor(Validate(request), "IconKey"));
    }

    // ---- ProductSearchQuery ----

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("kit", "name", "asc")]
    [InlineData("kit", "PRICE", "DESC")]
    public void Search_ValidValues_Pass(string? search, string? sortBy, string? sortDir)
    {
        ProductSearchQuery query = new ProductSearchQuery { Search = search, SortBy = sortBy, SortDir = sortDir };

        Assert.Empty(Validate(query));
    }

    [Fact]
    public void Search_Of50Characters_Passes_And51_Fails()
    {
        Assert.Empty(Validate(new ProductSearchQuery { Search = new string('s', 50) }));
        Assert.True(HasErrorFor(Validate(new ProductSearchQuery { Search = new string('s', 51) }), "Search"));
    }

    [Theory]
    [InlineData("category", "asc", "SortBy")]
    [InlineData("name", "up", "SortDir")]
    public void Search_UnknownSortValues_Fail(string sortBy, string sortDir, string failingField)
    {
        ProductSearchQuery query = new ProductSearchQuery { SortBy = sortBy, SortDir = sortDir };

        Assert.True(HasErrorFor(Validate(query), failingField));
    }

    // ---- WeatherOverrideRequest ----

    [Fact]
    public void WeatherOverride_KnownCondition_Passes()
    {
        Assert.Empty(Validate(new WeatherOverrideRequest { Condition = WeatherCondition.DustStorm }));
    }

    [Fact]
    public void WeatherOverride_MissingCondition_Fails()
    {
        Assert.True(HasErrorFor(Validate(new WeatherOverrideRequest { Condition = null }), "Condition"));
    }

    [Fact]
    public void WeatherOverride_UndefinedConditionNumber_Fails()
    {
        Assert.True(HasErrorFor(Validate(new WeatherOverrideRequest { Condition = (WeatherCondition)99 }), "Condition"));
    }
}
