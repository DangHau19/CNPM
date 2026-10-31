using AudioGuide.DAL;
using Xunit;

namespace AudioGuide.Tests;

public class DbConstraintsTests
{
    [Fact]
    public void ConstraintNames_AreUniqueAndValid()
    {
        var names = DbConstraints.All.Select(c => c.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(DbConstraints.All, c =>
        {
            Assert.InRange(c.Name.Length, 1, 64);
            Assert.False(string.IsNullOrWhiteSpace(c.Sql));
        });
    }

    [Fact]
    public void RadiusRule_MatchesBusinessRule()
    {
        var radius = DbConstraints.All.Single(c => c.Name == "CK_Places_Radius");
        Assert.Contains("BETWEEN 20 AND 2000", radius.Sql);
    }
}
