﻿using System;
using System.Configuration;
using Semver;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations.Upgrade.V_7_12_0;
using Umbraco.Core.Migrations.Upgrade.V_8_0_0;

namespace Umbraco.Core.Migrations.Upgrade
{
    /// <summary>
    /// Represents Umbraco's migration plan.
    /// </summary>
    public class UmbracoPlan : MigrationPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UmbracoPlan"/> class.
        /// </summary>
        public UmbracoPlan()
            : base(Constants.System.UmbracoUpgradePlanName)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UmbracoPlan"/> class.
        /// </summary>
        public UmbracoPlan(IMigrationBuilder migrationBuilder, ILogger logger)
            : base(Constants.System.UmbracoUpgradePlanName, migrationBuilder, logger)
        { }

        /// <inheritdoc />
        /// <remarks>
        /// <para>The default initial state in plans is string.Empty.</para>
        /// <para>When upgrading from version 7, we want to use specific initial states
        /// that are e.g. "{init-7.9.3}", "{init-7.11.1}", etc. so we can chain the proper
        /// migrations.</para>
        /// <para>This is also where we detect the current version, and reject invalid
        /// upgrades (from a tool old version, or going back in time, etc).</para>
        /// </remarks>
        public override string InitialState
        {
            get
            {
                // no state in database yet - assume we have something in web.config that makes some sense
                if (!SemVersion.TryParse(ConfigurationManager.AppSettings["umbracoConfigurationStatus"], out var currentVersion))
                    throw new InvalidOperationException("Could not get current version from web.config umbracoConfigurationStatus appSetting.");

                // we currently support upgrading from 7.10.0 and later
                if (currentVersion < new SemVersion(7, 10))
                    throw new InvalidOperationException($"Version {currentVersion} cannot be migrated to {UmbracoVersion.SemanticVersion}.");

                // cannot go back in time
                if (currentVersion > UmbracoVersion.SemanticVersion)
                    throw new InvalidOperationException($"Version {currentVersion} cannot be downgraded to {UmbracoVersion.SemanticVersion}.");

                // upgrading from version 7 => initial state is eg "{init-7.10.0}"
                // anything else is not supported - ie if 8 and above, we should have an initial state already
                if (currentVersion.Major != 7)
                    throw new InvalidOperationException($"Version {currentVersion} is not supported by the migration plan.");

                return "{init-" + currentVersion + "}";
            }
        }

        /// <inheritdoc />
        protected override void DefinePlan()
        {
            // MODIFYING THE PLAN
            //
            // Please take great care when modifying the plan!
            //
            // * Creating a migration for version 8:
            //     Append the migration to the main chain, using a new guid, before the "//FINAL" comment
            //
            //     If the new migration causes a merge conflict, because someone else also added another
            //     new migration, you NEED to fix the conflict by providing one default path, and paths
            //     out of the conflict states (see example below).
            //
            // * Porting from version 7:
            //     Append the ported migration to the main chain, using a new guid (same as above).
            //     Create a new special chain from the {init-...} state to the main chain (see example
            //     below).


            // plan starts at 7.10.0 (anything before 7.10.0 is not supported)
            // upgrades from 7 to 8, and then takes care of all eventual upgrades
            //
            From("{init-7.10.0}");
            Chain<AddLockObjects>("{7C447271-CA3F-4A6A-A913-5D77015655CB}");
            Chain<AddContentNuTable>("{CBFF58A2-7B50-4F75-8E98-249920DB0F37}");
            Chain<RefactorXmlColumns>("{3D18920C-E84D-405C-A06A-B7CEE52FE5DD}");
            Chain<VariantsMigration>("{FB0A5429-587E-4BD0-8A67-20F0E7E62FF7}");
            Chain<DropMigrationsTable>("{F0C42457-6A3B-4912-A7EA-F27ED85A2092}");
            Chain<DataTypeMigration>("{8640C9E4-A1C0-4C59-99BB-609B4E604981}");
            Chain<TagsMigration>("{DD1B99AF-8106-4E00-BAC7-A43003EA07F8}");
            Chain<SuperZero>("{9DF05B77-11D1-475C-A00A-B656AF7E0908}");
            Chain<PropertyEditorsMigration>("{6FE3EF34-44A0-4992-B379-B40BC4EF1C4D}");
            Chain<LanguageColumns>("{7F59355A-0EC9-4438-8157-EB517E6D2727}");

            // AddVariationTables1 has been superceeded by AddVariationTables2
            //Chain<AddVariationTables1>("{941B2ABA-2D06-4E04-81F5-74224F1DB037}");
            Chain<AddVariationTables2>("{76DF5CD7-A884-41A5-8DC6-7860D95B1DF5}");
            // however, provide a path out of the old state
            Add<AddVariationTables1A>("{941B2ABA-2D06-4E04-81F5-74224F1DB037}", "{76DF5CD7-A884-41A5-8DC6-7860D95B1DF5}");
            // resume at {76DF5CD7-A884-41A5-8DC6-7860D95B1DF5} ...

            Chain<RefactorMacroColumns>("{A7540C58-171D-462A-91C5-7A9AA5CB8BFD}");

            Chain<UserForeignKeys>("{3E44F712-E2E3-473A-AE49-5D7F8E67CE3F}");  // shannon added that one - let's keep it as the default path
            //Chain<AddTypedLabels>("{65D6B71C-BDD5-4A2E-8D35-8896325E9151}"); // stephan added that one = merge conflict, remove,
            Chain<AddTypedLabels>("{4CACE351-C6B9-4F0C-A6BA-85A02BBD39E4}");   // but it after shannon's, with a new target state,
            Add<UserForeignKeys>("{65D6B71C-BDD5-4A2E-8D35-8896325E9151}", "{4CACE351-C6B9-4F0C-A6BA-85A02BBD39E4}"); // and provide a path out of the conflict state
            // resume at {4CACE351-C6B9-4F0C-A6BA-85A02BBD39E4} ...

            Chain<ContentVariationMigration>("{1350617A-4930-4D61-852F-E3AA9E692173}");
            Chain<UpdateUmbracoConsent>("{39E5B1F7-A50B-437E-B768-1723AEC45B65}"); // from 7.12.0
            Chain<FallbackLanguage>("{CF51B39B-9B9A-4740-BB7C-EAF606A7BFBF}");
            //FINAL




            // and then, need to support upgrading from more recent 7.x
            //
            From("{init-7.10.1}").Chain("{init-7.10.0}"); // same as 7.10.0
            From("{init-7.10.2}").Chain("{init-7.10.0}"); // same as 7.10.0
            From("{init-7.10.3}").Chain("{init-7.10.0}"); // same as 7.10.0
            From("{init-7.10.4}").Chain("{init-7.10.0}"); // same as 7.10.0
            From("{init-7.11.0}").Chain("{init-7.10.0}"); // same as 7.10.0
            From("{init-7.11.1}").Chain("{init-7.10.0}"); // same as 7.10.0

            // 7.12.0 has a migration, define a custom chain which copies the chain
            // going from {init-7.10.0} to former final, and then goes straight to
            // main chain, skipping the migration
            //
            From("{init-7.12.0}");
            //        copy from        copy to (former final)                    main chain
            CopyChain("{init-7.10.0}", "{1350617A-4930-4D61-852F-E3AA9E692173}", "{39E5B1F7-A50B-437E-B768-1723AEC45B65}");
        }
    }
}
