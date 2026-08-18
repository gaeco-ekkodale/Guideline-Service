// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.IO;
using System.Linq;
using Guideline.Model.Enums;
using Guideline.Model.Model;

namespace GuidelineService.Tests;

public static class TestUtils
{
	public static IGuideline CreateGuideline()
	{
		var guideline = new Guideline.Model.Model.Guideline();

		const string pathToCSV = @"..\..\..\..\Testdata\Sofistik\Autobahn_GmbH_Merkmale.csv";
		const string complexDataItem1 = "SofistikTree";

		// sample complex item
		guideline.ComplexData.Items.Add(new ComplexDataItem() { Name = complexDataItem1, Path = pathToCSV });

		// sample domain
		var domain = new Domain() { Name = "ekkodale" };
		guideline.Domain = domain;

		const string RevitClassification = "OST_GenericModel";

		// sample parent classification
		var parentclass = new Classification() { Name = "Tragwerk" };
		guideline.Domain.Classifications.Add(parentclass);

		// sample classification
		var subclass = new Classification() { Name = "Stütze", Identifier = "STZ123" };
		guideline.Domain.Classifications.Add(subclass);
		parentclass.AddSubClassification(subclass);
		subclass.AddParentClassification(parentclass);

		var pset = new PropertySet() { Name = "MyPset" };

		//sample Mapping
		var mapping = new Mapping()
		{
			SourceApplication = SourceSystems.Revit,
			ClassificationMap = new ClassificationMapping()
			{
				SourceClassificationName = RevitClassification,
				TargetClassification = subclass,
				SourceClassificationParameterName = "MyClassificationParam",
				SourceClassificationParameterValue = subclass.Name,
			}
		};
		guideline.Mappings.Add(mapping);

		//sample Mapping
		var mappingAsterixSubmapping = new Mapping()
		{
			SourceApplication = SourceSystems.Revit,
			ClassificationMap = new ClassificationMapping()
			{
				SourceClassificationName = "*",
				TargetClassification = subclass,
				SourceClassificationParameterName = "MyClassificationParam",
				SourceClassificationParameterValue = subclass.Name,
			}
		};
		guideline.Mappings.Add(mappingAsterixSubmapping);


		// sample asterix mapping
		var asterixMapping = new Mapping()
		{
			SourceApplication = SourceSystems.Revit,
			ClassificationMap = new ClassificationMapping()
			{
				SourceClassificationName = "*",
				TargetClassification = subclass
			}
		};
		guideline.Mappings.Add(asterixMapping);


		// some sample properties
		var prop = new PropertySimple()
		{
			Name = "Länge",
			StorageType = StorageType.Real,
			UnitType = typeof(UnitsNet.Length).FullName,
			UnitAbbreviation = "cm"
		};
		guideline.Domain.Properties.Add(prop);
		var classprop = new ClassificationProperty()
		{
			PropertySet = pset,
			PropertyAssignment = new PropertySimpleAssignment()
			{
				Property = prop
			}
		};
		subclass.ClassificationProperties.Add(classprop);

		// Enum
		var propEnum = new PropertyEnum()
		{
			Name = "Enum",
			StorageType = StorageType.String,
		};

		domain.Properties.Add(propEnum);
		var classpropEnum = new ClassificationProperty()
		{
			PropertySet = pset,
			PropertyAssignment = new PropertyEnumAssignment()
			{
				Property = propEnum
			}
		};
		subclass.ClassificationProperties.Add(classpropEnum);

		// SuperEnum
		// Objektgruppe
		// Objekttyp
		var propSuperEnumObjectgruppe = new PropertySuperEnum()
		{
			Name = "Objektgruppe",
			StorageType = StorageType.String,
			Item = guideline.ComplexData.Items.FirstOrDefault(x => x.Name == complexDataItem1) as ComplexDataItem,
			Level = 1,
		};
		domain.Properties.Add(propSuperEnumObjectgruppe);
		var classpropSuperEnumObjektgruppe = new ClassificationProperty()
		{
			PropertySet = pset,
			PropertyAssignment = new PropertySuperEnumAssignment()
			{
				Property = propSuperEnumObjectgruppe
			}
		};
		subclass.ClassificationProperties.Add(classpropSuperEnumObjektgruppe);

		//Assign parameter mappings
		mapping.ParameterMappings.Add(new ParameterMapping()
		{
			SourceParameter = propSuperEnumObjectgruppe.Name,
			TargetParameter = classpropSuperEnumObjektgruppe,
			SourceParameterValueType = "TEXT",
			IsBuiltIn = false,
			UsageType = UsageType.Instance
		});

		// Objektklasse
		// Objekttyp
		var propSuperEnumObjektklasse = new PropertySuperEnum()
		{
			Name = "Objektklasse",
			StorageType = StorageType.String,
			Item = guideline.ComplexData.Items.FirstOrDefault(x => x.Name == complexDataItem1) as ComplexDataItem,
			Level = 2,
		};
		domain.Properties.Add(propSuperEnumObjektklasse);
		var classpropSuperEnumObjektklasse = new ClassificationProperty()
		{
			PropertySet = pset,
			PropertyAssignment = new PropertySuperEnumAssignment()
			{
				Parent = classpropSuperEnumObjektgruppe,
				Property = propSuperEnumObjektklasse
			}
		};
		subclass.ClassificationProperties.Add(classpropSuperEnumObjektklasse);

		//Assign parameter mappings
		mapping.ParameterMappings.Add(new ParameterMapping()
		{
			SourceParameter = propSuperEnumObjektklasse.Name,
			TargetParameter = classpropSuperEnumObjektklasse,
			SourceParameterValueType = "TEXT",
			IsBuiltIn = false,
			UsageType = UsageType.Instance
		});

		// Objekttyp
		var propSuperEnumObjekttyp = new PropertySuperEnum()
		{
			Name = "Objekttyp",
			StorageType = StorageType.String,
			Item = guideline.ComplexData.Items.FirstOrDefault(x => x.Name == complexDataItem1) as ComplexDataItem,
			Level = 3,
		};
		domain.Properties.Add(propSuperEnumObjekttyp);
		var classpropSuperEnumObjekttyp = new ClassificationProperty()
		{
			PropertySet = pset,
			PropertyAssignment = new PropertySuperEnumAssignment()
			{
				Parent = classpropSuperEnumObjektklasse,
				Property = propSuperEnumObjekttyp
			}
		};
		subclass.ClassificationProperties.Add(classpropSuperEnumObjekttyp);

		//Assign parameter mappings
		mapping.ParameterMappings.Add(new ParameterMapping()
		{
			SourceParameter = propSuperEnumObjekttyp.Name,
			TargetParameter = classpropSuperEnumObjekttyp,
			SourceParameterValueType = "TEXT",
			IsBuiltIn = false,
			UsageType = UsageType.Instance
		});

		return guideline;
	}

	public static string GetFileNameAndPath()
	{
		string filename = "TestGuideline.json";
		return Path.Combine(Path.GetTempPath(), filename);
	}

	public static void WriteGuidelineToFile(IGuideline guideline)
	{
		var handler = new GuidelineModelIO.GuidelineReaderWriter();
		handler.GuidelineWrite(guideline, GetFileNameAndPath());
	}
}
