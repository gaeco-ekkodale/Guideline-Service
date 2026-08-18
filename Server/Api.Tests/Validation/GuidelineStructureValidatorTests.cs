// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.Linq;
using Guideline.Model.Model;
using GuidelineService.Api.Validation;
using Xunit;

namespace GuidelineService.Tests.Validation;

public class GuidelineStructureValidatorTests
{
	#region Helper Methods

	/// <summary>
	/// Creates a minimal valid guideline with required fields populated.
	/// </summary>
	private static Guideline.Model.Model.Guideline CreateValidGuideline()
	{
		var guideline = new Guideline.Model.Model.Guideline();
		var domain = new Domain { Name = "TestDomain" };
		guideline.Domain = domain;

		var classification = new Classification { Name = "TestClassification" };
		domain.Classifications.Add(classification);

		var prop = new PropertySimple
		{
			Name = "TestProperty",
			StorageType = Guideline.Model.Enums.StorageType.String
		};
		domain.Properties.Add(prop);

		var cp = new ClassificationProperty
		{
			PropertyAssignment = new PropertySimpleAssignment { Property = prop }
		};
		classification.ClassificationProperties.Add(cp);

		return guideline;
	}

	#endregion

	[Fact]
	public void Validate_NullGuideline_ReturnsError()
	{
		var result = GuidelineStructureValidator.Validate(null);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, e => e.Contains("null"));
	}

	[Fact]
	public void Validate_NullGuidelineId_ReturnsError()
	{
		var guideline = CreateValidGuideline();
		guideline.Identifier = null;

		var result = GuidelineStructureValidator.Validate(guideline);

		Assert.Contains(result.Errors, e => e.Contains("Guideline Identifier"));
	}

	[Fact]
	public void Validate_EmptyGuidelineId_ReturnsError()
	{
		var guideline = CreateValidGuideline();
		guideline.Identifier = "";

		var result = GuidelineStructureValidator.Validate(guideline);

		Assert.Contains(result.Errors, e => e.Contains("Guideline Identifier"));
	}

	[Fact]
	public void Validate_NullDomain_ReturnsError()
	{
		var guideline = new Guideline.Model.Model.Guideline();
		guideline.Domain = null!;

		var result = GuidelineStructureValidator.Validate(guideline);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, e => e.Contains("Domain is null"));
	}

	[Fact]
	public void Validate_NullClassifications_ReturnsError()
	{
		var guideline = new Guideline.Model.Model.Guideline();
		guideline.Domain = new Domain { Name = "Test" };
		guideline.Domain.Classifications.Clear();

		var result = GuidelineStructureValidator.Validate(guideline);

		Assert.Contains(result.Errors, e => e.Contains("no classifications"));
	}

	[Fact]
	public void Validate_EmptyClassifications_ReturnsError()
	{
		var guideline = new Guideline.Model.Model.Guideline();
		guideline.Domain = new Domain { Name = "Test" };
		// Classifications is empty by default

		var result = GuidelineStructureValidator.Validate(guideline);

		Assert.Contains(result.Errors, e => e.Contains("no classifications"));
	}

	[Fact]
	public void Validate_ClassificationWithNullId_ReturnsError()
	{
		var guideline = CreateValidGuideline();
		guideline.Identifier = "valid-id";
		var noIdClassification = new Classification { Name = "NoId" };
		noIdClassification.Identifier = null;
		guideline.Domain.Classifications.Add(noIdClassification);

		var result = GuidelineStructureValidator.Validate(guideline);

		Assert.Contains(result.Errors, e => e.Contains("Classification") && e.Contains("null or empty Identifier"));
	}

	[Fact]
	public void Validate_DuplicateClassificationIds_ReturnsError()
	{
		var guideline = CreateValidGuideline();
		guideline.Identifier = "valid-id";
		guideline.Domain.Classifications.Clear();

		var cls1 = new Classification { Name = "Cls1" };
		cls1.Identifier = "shared-id";
		var cls2 = new Classification { Name = "Cls2" };
		cls2.Identifier = "shared-id";

		guideline.Domain.Classifications.Add(cls1);
		guideline.Domain.Classifications.Add(cls2);

		var result = GuidelineStructureValidator.Validate(guideline);

		Assert.Contains(result.Errors, e => e.Contains("Duplicate Classification Identifier"));
	}

	[Fact]
	public void Validate_PropertyWithNullId_ReturnsError()
	{
		var guideline = CreateValidGuideline();
		guideline.Identifier = "valid-id";
		var prop = new PropertySimple { Name = "NullIdProp" };
		prop.Identifier = null;
		guideline.Domain.Properties.Add(prop);

		var result = GuidelineStructureValidator.Validate(guideline);

		Assert.Contains(result.Errors, e => e.Contains("Property") && e.Contains("null or empty Identifier"));
	}

	[Fact]
	public void Validate_DuplicatePropertyIds_ReturnsError()
	{
		var guideline = CreateValidGuideline();
		guideline.Identifier = "valid-id";
		var prop1 = guideline.Domain.Properties.First();
		var prop2 = new PropertySimple { Name = "DupProp" };
		prop2.Identifier = prop1.Identifier;
		guideline.Domain.Properties.Add(prop2);

		var result = GuidelineStructureValidator.Validate(guideline);

		Assert.Contains(result.Errors, e => e.Contains("Duplicate Property Identifier"));
	}

	[Fact]
	public void Validate_PropertySetWithNullId_ReturnsError()
	{
		var guideline = CreateValidGuideline();
		guideline.Identifier = "valid-id";
		var ps = new PropertySet { Name = "NullIdPs" };
		ps.Identifier = null;
		guideline.Domain.PropertySets.Add(ps);

		var result = GuidelineStructureValidator.Validate(guideline);

		Assert.Contains(result.Errors, e => e.Contains("PropertySet") && e.Contains("null or empty Identifier"));
	}

	[Fact]
	public void Validate_DuplicatePropertySetIds_ReturnsError()
	{
		var guideline = CreateValidGuideline();
		guideline.Identifier = "valid-id";
		var ps1 = new PropertySet { Name = "Ps1" };
		var ps2 = new PropertySet { Name = "Ps2" };
		ps2.Identifier = ps1.Identifier;
		guideline.Domain.PropertySets.Add(ps1);
		guideline.Domain.PropertySets.Add(ps2);

		var result = GuidelineStructureValidator.Validate(guideline);

		Assert.Contains(result.Errors, e => e.Contains("Duplicate PropertySet Identifier"));
	}

	[Fact]
	public void Validate_ClassificationPropertyWithNullId_ReturnsError()
	{
		var guideline = CreateValidGuideline();
		guideline.Identifier = "valid-id";
		var cp = new ClassificationProperty();
		cp.Identifier = null;
		guideline.Domain.Classifications.First().ClassificationProperties.Add(cp);

		var result = GuidelineStructureValidator.Validate(guideline);

		Assert.Contains(result.Errors, e => e.Contains("ClassificationProperty") && e.Contains("null or empty Identifier"));
	}

	//[Fact]
	//public void Validate_DuplicateClassificationPropertyIds_ReturnsError()
	//{
	//    var guideline = CreateValidGuideline();
	//    guideline.Identifier = "valid-id";
	//    var existingCp = guideline.Domain.Classifications.First().ClassificationProperties.First();
	//    var dupCp = new ClassificationProperty();
	//    dupCp.Identifier = existingCp.Identifier;
	//    guideline.Domain.Classifications.First().ClassificationProperties.Add(dupCp);

	//    var result = GuidelineStructureValidator.Validate(guideline);

	//    Assert.Contains(result.Errors, e => e.Contains("Duplicate ClassificationProperty Identifier"));
	//}

	[Fact]
	public void Validate_ValidGuideline_ReturnsSuccess()
	{
		var guideline = CreateValidGuideline();

		var result = GuidelineStructureValidator.Validate(guideline);

		Assert.True(result.IsValid);
		Assert.Empty(result.Errors);
	}

	[Fact]
	public void Validate_MultipleErrors_ReturnsAllErrors()
	{
		var guideline = new Guideline.Model.Model.Guideline();
		guideline.Identifier = null;
		// null Identifier + domain with no classifications → 2 errors
		guideline.Domain = new Domain { Name = "Test" };

		var result = GuidelineStructureValidator.Validate(guideline);

		Assert.False(result.IsValid);
		Assert.True(result.Errors.Count >= 2, $"Expected at least 2 errors but got {result.Errors.Count}: {string.Join("; ", result.Errors)}");
	}
}
