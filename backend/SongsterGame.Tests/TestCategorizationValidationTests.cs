using System.Reflection;
using Xunit.Categories;

namespace SongsterGame.Tests;

/// <summary>
/// Validation tests to ensure all test methods in the solution are properly categorized
/// as either UnitTest or IntegrationTest.
/// </summary>
public class TestCategorizationValidationTests
{
    private const string UnitTestCategory = "UnitTest";
    private const string IntegrationTestCategory = "IntegrationTest";

    [Fact]
    [UnitTest]
    public void AllTests_ShouldHave_UnitTestOrIntegrationTestCategory()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var testMethods = GetAllTestMethods(assembly);
        var uncategorizedTests = new List<string>();

        // Act
        foreach (var method in testMethods)
        {
            var hasUnitTestAttribute = method.GetCustomAttributes(typeof(UnitTestAttribute), inherit: true).Any();
            var hasIntegrationTestAttribute = method.GetCustomAttributes(typeof(IntegrationTestAttribute), inherit: true).Any();

            if (!hasUnitTestAttribute && !hasIntegrationTestAttribute)
            {
                uncategorizedTests.Add($"{method.DeclaringType?.FullName}.{method.Name}");
            }
        }

        // Assert
        Assert.Empty(uncategorizedTests);
    }

    [Fact]
    [UnitTest]
    public void NoTest_ShouldHave_BothUnitTestAndIntegrationTestCategories()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var testMethods = GetAllTestMethods(assembly);
        var duplicateCategorizedTests = new List<string>();

        // Act
        foreach (var method in testMethods)
        {
            var hasUnitTestAttribute = method.GetCustomAttributes(typeof(UnitTestAttribute), inherit: true).Any();
            var hasIntegrationTestAttribute = method.GetCustomAttributes(typeof(IntegrationTestAttribute), inherit: true).Any();

            if (hasUnitTestAttribute && hasIntegrationTestAttribute)
            {
                duplicateCategorizedTests.Add($"{method.DeclaringType?.FullName}.{method.Name}");
            }
        }

        // Assert
        Assert.Empty(duplicateCategorizedTests);
    }

    [Fact]
    [UnitTest]
    public void UnitTests_ShouldBeIn_UnitTestFolder()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var testMethods = GetAllTestMethods(assembly);
        var misplacedTests = new List<string>();

        // Act
        foreach (var method in testMethods)
        {
            var hasUnitTestAttribute = method.GetCustomAttributes(typeof(UnitTestAttribute), inherit: true).Any();
            var declaringType = method.DeclaringType;

            if (hasUnitTestAttribute && declaringType != null)
            {
                var namespaceName = declaringType.Namespace ?? string.Empty;
                if (!namespaceName.Contains(".Unit") && declaringType.Name != nameof(TestCategorizationValidationTests))
                {
                    misplacedTests.Add($"{declaringType.FullName}.{method.Name} (Expected namespace to contain '.Unit')");
                }
            }
        }

        // Assert
        Assert.Empty(misplacedTests);
    }

    [Fact]
    [UnitTest]
    public void IntegrationTests_ShouldBeIn_IntegrationTestFolder()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var testMethods = GetAllTestMethods(assembly);
        var misplacedTests = new List<string>();

        // Act
        foreach (var method in testMethods)
        {
            var hasIntegrationTestAttribute = method.GetCustomAttributes(typeof(IntegrationTestAttribute), inherit: true).Any();
            var declaringType = method.DeclaringType;

            if (hasIntegrationTestAttribute && declaringType != null)
            {
                var namespaceName = declaringType.Namespace ?? string.Empty;
                if (!namespaceName.Contains(".Integration"))
                {
                    misplacedTests.Add($"{declaringType.FullName}.{method.Name} (Expected namespace to contain '.Integration')");
                }
            }
        }

        // Assert
        Assert.Empty(misplacedTests);
    }

    private static IEnumerable<MethodInfo> GetAllTestMethods(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m.GetCustomAttributes(typeof(FactAttribute), inherit: true).Any()
                     || m.GetCustomAttributes(typeof(TheoryAttribute), inherit: true).Any());
    }
}
