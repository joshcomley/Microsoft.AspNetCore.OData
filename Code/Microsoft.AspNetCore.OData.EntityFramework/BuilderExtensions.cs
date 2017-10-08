using System.Linq;
using Microsoft.AspNetCore.OData.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;

namespace Microsoft.AspNetCore.OData.EntityFramework
{
    public static class ModelBuilderExtensions
    {
        public static void BuildNavigationPropertiesFromOData(this ModelBuilder builder, ODataModelBuilder odataModel)
        {
            //builder.Entity<SiteInspection>()
            //    .HasOne(u => u.RiskAssessment)
            //    .WithOne(u => u.SiteInspection)
            //    .HasForeignKey<SiteInspection>(c => c.RiskAssessmentId)
            //    .HasPrincipalKey<RiskAssessment>(e => e.Id)
            //    ;

            foreach (var entitySet in odataModel.EntitySets)
            {
                if (entitySet.EntityType.Keys.Any())
                {
                    builder.Entity(entitySet.ClrType)
                        .HasKey(entitySet.EntityType.Keys.Select(k => k.Name).ToArray());
                }
                foreach (var navigationProperty in entitySet.EntityType.NavigationProperties)
                {
                    var dependentProperties = navigationProperty.DependentProperties.Select(p => p.Name).ToArray();
                    var principalProperties = navigationProperty.PrincipalProperties.Select(p => p.Name).ToArray();
                    var partnerDependentProperties = navigationProperty.Partner?.DependentProperties.Select(p => p.Name).ToArray() ?? new string[] { };
                    var partnerPrincipalProperties = navigationProperty.Partner?.PrincipalProperties.Select(p => p.Name).ToArray() ?? new string[] { };
                    var dependentEntityTypeName = navigationProperty.RelatedClrType.Name;
                    var principalEntityTypeName = entitySet.ClrType.Name;
                    switch (navigationProperty.Multiplicity)
                    {
                        case EdmMultiplicity.ZeroOrOne:
                        case EdmMultiplicity.One:
                            var referenceBuilder =
                                builder
                                    .Entity(entitySet.ClrType)
                                    .HasOne(navigationProperty.RelatedClrType, navigationProperty.Name);
                            builder.Entity(navigationProperty.RelatedClrType);
                            if (navigationProperty.Partner != null)
                            {
                                switch (navigationProperty.Partner.Multiplicity)
                                {
                                    case EdmMultiplicity.One:
                                        var referenceReferenceBuilder =
                                            referenceBuilder.WithOne(navigationProperty.Partner.Name);
                                        if (partnerPrincipalProperties.Any())
                                        {
                                            referenceReferenceBuilder.HasForeignKey(
                                                principalEntityTypeName,
                                                partnerPrincipalProperties);
                                        }
                                        if (partnerDependentProperties.Any())
                                        {
                                            referenceReferenceBuilder.HasPrincipalKey(
                                                dependentEntityTypeName,
                                                partnerDependentProperties);
                                        }
                                        switch (navigationProperty.OnDeleteAction)
                                        {
                                            case EdmOnDeleteAction.Cascade:
                                                referenceReferenceBuilder.OnDelete(DeleteBehavior.Cascade);
                                                break;
                                            case EdmOnDeleteAction.None:
                                                referenceReferenceBuilder.OnDelete(DeleteBehavior.ClientSetNull);
                                                break;
                                        }
                                        break;
                                    case EdmMultiplicity.Many:
                                        var referenceCollectionBuilder =
                                            referenceBuilder.WithMany(navigationProperty.Partner.Name);
                                        if (dependentProperties.Any())
                                        {
                                            referenceCollectionBuilder.HasForeignKey(
                                                dependentProperties);
                                        }
                                        if (principalProperties.Any())
                                        {
                                            referenceCollectionBuilder.HasPrincipalKey(
                                                principalProperties);
                                        }
                                        switch (navigationProperty.OnDeleteAction)
                                        {
                                            case EdmOnDeleteAction.Cascade:
                                                referenceCollectionBuilder.OnDelete(DeleteBehavior.Cascade);
                                                break;
                                            case EdmOnDeleteAction.None:
                                                referenceCollectionBuilder.OnDelete(DeleteBehavior.ClientSetNull);
                                                break;
                                        }
                                        break;
                                }
                            }
                            break;
                        case EdmMultiplicity.Many:
                            var collectionBuilder =
                                builder
                                    .Entity(entitySet.ClrType)
                                    .HasMany(navigationProperty.RelatedClrType, navigationProperty.Name);
                            if (navigationProperty.Partner != null)
                            {
                                switch (navigationProperty.Partner.Multiplicity)
                                {
                                    case EdmMultiplicity.One:
                                        var referenceCollectionBuilder =
                                            collectionBuilder.WithOne(navigationProperty.Partner.Name);
                                        if (dependentProperties.Any())
                                        {
                                            referenceCollectionBuilder.HasPrincipalKey(
                                                dependentProperties);
                                        }
                                        if (principalProperties.Any())
                                        {
                                            referenceCollectionBuilder.HasForeignKey(
                                                principalProperties);
                                        }
                                        switch (navigationProperty.OnDeleteAction)
                                        {
                                            case EdmOnDeleteAction.Cascade:
                                                referenceCollectionBuilder.OnDelete(DeleteBehavior.Cascade);
                                                break;
                                            case EdmOnDeleteAction.None:
                                                referenceCollectionBuilder.OnDelete(DeleteBehavior.ClientSetNull);
                                                break;
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }
            }
        }
    }
}