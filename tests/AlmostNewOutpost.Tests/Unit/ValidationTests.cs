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
        // Arrange
        LoginRequest request = new LoginRequest
        {
            Username = new string('u', usernameLength),
            Password = new string('p', passwordLength),
        };

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("", "secret", "Username")]
    [InlineData("   ", "secret", "Username")]
    [InlineData("medic", "", "Password")]
    public void Login_BlankFields_Fail(string username, string password, string failingField)
    {
        // Arrange
        LoginRequest request = new LoginRequest { Username = username, Password = password };

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.True(HasErrorFor(results, failingField));
    }

    [Fact]
    public void Login_TooLong_Fails()
    {
        // Arrange
        LoginRequest request = new LoginRequest { Username = new string('u', 51), Password = new string('p', 129) };

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.True(HasErrorFor(results, "Username"));
        Assert.True(HasErrorFor(results, "Password"));
    }

    // ---- CreatePurchaseRequest ----

    [Fact]
    public void Purchase_ProductIdOne_Passes()
    {
        // Arrange
        CreatePurchaseRequest request = new CreatePurchaseRequest { ProductId = 1 };

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public void Purchase_MissingOrTooSmallProductId_Fails(int? productId)
    {
        // Arrange
        CreatePurchaseRequest request = new CreatePurchaseRequest { ProductId = productId };

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.True(HasErrorFor(results, "ProductId"));
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
        // Arrange
        ProductUpsertRequest request = ValidProduct();

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100000)]
    public void Product_PriceOnTheBoundary_Passes(int price)
    {
        // Arrange
        ProductUpsertRequest request = ValidProduct();
        request.PriceCredits = price;

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(100001)]
    public void Product_PriceOutOfRange_Fails(int? price)
    {
        // Arrange
        ProductUpsertRequest request = ValidProduct();
        request.PriceCredits = price;

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.True(HasErrorFor(results, "PriceCredits"));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("  ab  ")]
    public void Product_NameOfTwoCharactersAfterTrimming_Passes(string name)
    {
        // Arrange
        ProductUpsertRequest request = ValidProduct();
        request.Name = name;

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("  a  ")]
    public void Product_NameTooShort_Fails(string name)
    {
        // Arrange
        ProductUpsertRequest request = ValidProduct();
        request.Name = name;

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.True(HasErrorFor(results, "Name"));
    }

    [Fact]
    public void Product_NameOf100Characters_Passes_And101_Fails()
    {
        // Arrange
        ProductUpsertRequest ok = ValidProduct();
        ok.Name = new string('n', 100);
        ProductUpsertRequest tooLong = ValidProduct();
        tooLong.Name = new string('n', 101);

        // Act
        List<ValidationResult> okResults = Validate(ok);
        List<ValidationResult> tooLongResults = Validate(tooLong);

        // Assert
        Assert.Empty(okResults);
        Assert.True(HasErrorFor(tooLongResults, "Name"));
    }

    [Fact]
    public void Product_DescriptionOf500Characters_Passes_And501_Fails()
    {
        // Arrange
        ProductUpsertRequest ok = ValidProduct();
        ok.Description = new string('d', 500);
        ProductUpsertRequest tooLong = ValidProduct();
        tooLong.Description = new string('d', 501);

        // Act
        List<ValidationResult> okResults = Validate(ok);
        List<ValidationResult> tooLongResults = Validate(tooLong);

        // Assert
        Assert.Empty(okResults);
        Assert.True(HasErrorFor(tooLongResults, "Description"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void Product_MissingCategory_Fails(int? categoryId)
    {
        // Arrange
        ProductUpsertRequest request = ValidProduct();
        request.CategoryId = categoryId;

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.True(HasErrorFor(results, "CategoryId"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("first-aid")]
    [InlineData("Icon2")]
    public void Product_IconKey_AllowedValues_Pass(string? iconKey)
    {
        // Arrange
        ProductUpsertRequest request = ValidProduct();
        request.IconKey = iconKey;

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("first aid")]
    [InlineData("icon_key")]
    [InlineData("<script>")]
    public void Product_IconKey_WithOtherCharacters_Fails(string iconKey)
    {
        // Arrange
        ProductUpsertRequest request = ValidProduct();
        request.IconKey = iconKey;

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.True(HasErrorFor(results, "IconKey"));
    }

    [Fact]
    public void Product_IconKeyOf51Characters_Fails()
    {
        // Arrange
        ProductUpsertRequest request = ValidProduct();
        request.IconKey = new string('k', 51);

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.True(HasErrorFor(results, "IconKey"));
    }

    // ---- ProductSearchQuery ----

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("kit", "name", "asc")]
    [InlineData("kit", "PRICE", "DESC")]
    public void Search_ValidValues_Pass(string? search, string? sortBy, string? sortDir)
    {
        // Arrange
        ProductSearchQuery query = new ProductSearchQuery { Search = search, SortBy = sortBy, SortDir = sortDir };

        // Act
        List<ValidationResult> results = Validate(query);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Search_Of50Characters_Passes_And51_Fails()
    {
        // Arrange
        ProductSearchQuery ok = new ProductSearchQuery { Search = new string('s', 50) };
        ProductSearchQuery tooLong = new ProductSearchQuery { Search = new string('s', 51) };

        // Act
        List<ValidationResult> okResults = Validate(ok);
        List<ValidationResult> tooLongResults = Validate(tooLong);

        // Assert
        Assert.Empty(okResults);
        Assert.True(HasErrorFor(tooLongResults, "Search"));
    }

    [Theory]
    [InlineData("category", "asc", "SortBy")]
    [InlineData("name", "up", "SortDir")]
    public void Search_UnknownSortValues_Fail(string sortBy, string sortDir, string failingField)
    {
        // Arrange
        ProductSearchQuery query = new ProductSearchQuery { SortBy = sortBy, SortDir = sortDir };

        // Act
        List<ValidationResult> results = Validate(query);

        // Assert
        Assert.True(HasErrorFor(results, failingField));
    }

    // ---- WeatherOverrideRequest ----

    [Fact]
    public void WeatherOverride_KnownCondition_Passes()
    {
        // Arrange
        WeatherOverrideRequest request = new WeatherOverrideRequest { Condition = WeatherCondition.DustStorm };

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void WeatherOverride_MissingCondition_Fails()
    {
        // Arrange
        WeatherOverrideRequest request = new WeatherOverrideRequest { Condition = null };

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.True(HasErrorFor(results, "Condition"));
    }

    [Fact]
    public void WeatherOverride_UndefinedConditionNumber_Fails()
    {
        // Arrange
        WeatherOverrideRequest request = new WeatherOverrideRequest { Condition = (WeatherCondition)99 };

        // Act
        List<ValidationResult> results = Validate(request);

        // Assert
        Assert.True(HasErrorFor(results, "Condition"));
    }
}
