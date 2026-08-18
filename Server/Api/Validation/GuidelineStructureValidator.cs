// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace GuidelineService.Api.Validation;

/// <summary>
/// Performs fast structural validation on a deserialized guideline
/// to reject invalid data before storage and event publishing.
/// </summary>
public static class GuidelineStructureValidator
{
	/// <summary>
	/// Validates the structural integrity of a guideline, collecting all errors
	/// rather than failing on the first one.
	/// </summary>
	/// <param name="guideline">The deserialized guideline to validate.</param>
	/// <returns>A result containing all validation errors, if any.</returns>
	public static GuidelineValidationResult Validate(Guideline.Model.Model.Guideline? guideline)
	{
		var result = new GuidelineValidationResult();

		if (guideline == null)
		{
			result.Errors.Add("Guideline is null.");
			return result;
		}

		if (string.IsNullOrEmpty(guideline.Identifier))
		{
			result.Errors.Add("Guideline Identifier is null or empty.");
		}

		if (guideline.Domain == null)
		{
			result.Errors.Add("Guideline Domain is null.");
			return result;
		}

		ValidateClassifications(guideline.Domain, result);
		ValidateProperties(guideline.Domain, result);
		ValidatePropertySets(guideline.Domain, result);

		return result;
	}

	private static void ValidateClassifications(Guideline.Model.Model.IDomain domain, GuidelineValidationResult result)
	{
		if (domain.Classifications == null || domain.Classifications.Count == 0)
		{
			result.Errors.Add("Guideline Domain has no classifications.");
			return;
		}

		var seenIds = new HashSet<string>(StringComparer.Ordinal);

		foreach (var classification in domain.Classifications)
		{
			if (string.IsNullOrEmpty(classification.Identifier))
			{
				result.Errors.Add($"Classification has a null or empty Identifier (Name: '{classification.Name ?? "unknown"}').");
			}
			else if (!seenIds.Add(classification.Identifier))
			{
				result.Errors.Add($"Duplicate Classification Identifier '{classification.Identifier}'.");
			}

			ValidateClassificationProperties(classification, result);
		}
	}

	private static void ValidateClassificationProperties(Guideline.Model.Model.IClassification classification, GuidelineValidationResult result)
	{
		if (classification.ClassificationProperties == null)
		{
			return;
		}

		var seenIds = new HashSet<string>(StringComparer.Ordinal);

		foreach (var cp in classification.ClassificationProperties)
		{
			if (string.IsNullOrEmpty(cp.Identifier))
			{
				result.Errors.Add($"ClassificationProperty has a null or empty Identifier in Classification '{classification.Identifier ?? classification.Name ?? "unknown"}'.");
			}
			//else if (!seenIds.Add(cp.Identifier))
			//{
			//	result.Errors.Add($"Duplicate ClassificationProperty Identifier '{cp.Identifier}' in Classification '{classification.Identifier ?? classification.Name ?? "unknown"}'.");
			//}
		}
	}

	private static void ValidateProperties(Guideline.Model.Model.IDomain domain, GuidelineValidationResult result)
	{
		if (domain.Properties == null)
		{
			return;
		}

		var seenIds = new HashSet<string>(StringComparer.Ordinal);

		foreach (var prop in domain.Properties)
		{
			if (string.IsNullOrEmpty(prop.Identifier))
			{
				result.Errors.Add($"Property has a null or empty Identifier (Name: '{prop.Name ?? "unknown"}').");
			}
			else if (!seenIds.Add(prop.Identifier))
			{
				result.Errors.Add($"Duplicate Property Identifier '{prop.Identifier}'.");
			}
		}
	}

	private static void ValidatePropertySets(Guideline.Model.Model.IDomain domain, GuidelineValidationResult result)
	{
		if (domain.PropertySets == null)
		{
			return;
		}

		var seenIds = new HashSet<string>(StringComparer.Ordinal);

		foreach (var ps in domain.PropertySets)
		{
			if (string.IsNullOrEmpty(ps.Identifier))
			{
				result.Errors.Add($"PropertySet has a null or empty Identifier (Name: '{ps.Name ?? "unknown"}').");
			}
			else if (!seenIds.Add(ps.Identifier))
			{
				result.Errors.Add($"Duplicate PropertySet Identifier '{ps.Identifier}'.");
			}
		}
	}
}

/// <summary>
/// Contains the result of guideline structural validation.
/// </summary>
public class GuidelineValidationResult
{
	/// <summary>
	/// Gets a value indicating whether the guideline passed all validation checks.
	/// </summary>
	public bool IsValid => Errors.Count == 0;

	/// <summary>
	/// Gets the list of validation error messages.
	/// </summary>
	public List<string> Errors { get; } = new();
}
