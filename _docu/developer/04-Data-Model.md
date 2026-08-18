# Data Model

This document describes the data models of the Guideline Service.

# Core Models

## Index / IIndex

Provides a base entity with a single, unique identifier.

### Properties

-   **ID** (`string`): A unique identifier for the entity, initialized to a new GUID by default.

## BaseClass / IBaseClass

A fundamental class for most domain models that provides common identifying and metadata properties. It inherits from `Index`.

### Properties

-   **ID** (`string`): Inherited from `Index`. A unique identifier for the entity.
-   **Name** (`string`): The primary display name of the entity.
-   **Description** (`string`): A detailed description of the entity's purpose.
-   **Identifier** (`string`): A secondary unique identifier, separate from the primary `ID`, also initialized to a new GUID.
-   **Definition** (`string`): A formal definition for the entity.
-   **Status** (`Status`): The current status of the entity (e.g., `Active` or `Inactive`). Defaults to `Active`.
-   **Version** (`string`): A version string for the entity.

## GuidelineCollection / IGuidelineCollection

A container for a list of `IGuideline` objects, providing helper methods to query the collection.

### Properties

-   **Guidelines** (`ICollection<IGuideline>`): A collection of guideline objects.

## Guideline / IGuideline

The root object representing a complete guideline. It aggregates the domain model (`Domain`), complex data structures (`ComplexData`), and all associated mappings (`Mappings`).

### Properties

-   **Domain** (`IDomain`): The core domain model of the guideline, containing all classifications, properties, and property sets.
-   **ComplexData** (`ComplexData`): A container for hierarchical data structures used by certain property types like `PropertySuperEnum`.
-   **Mappings** (`ICollection<IMapping>`): A collection of mapping rules that define how data from external systems corresponds to the guideline's domain model.

## Domain / IDomain

Acts as a container for the core components of a guideline's data model.

### Properties

-   **Classifications** (`ICollection<IClassification>`): The collection of all classifications defined within the guideline.
-   **Properties** (`ICollection<IProperty>`): The collection of all reusable property definitions.
-   **PropertySets** (`ICollection<IPropertySet>`): The collection of all property sets, which are used to group properties.

# Classification Models

## Classification / IClassification

Represents a specific classification within the guideline's domain model, organized in a hierarchical structure.

### Properties

-   **Parent** (`IClassificationRelation`): A wrapper object that points to the parent classification in the hierarchy.
-   **Children** (`ICollection<IClassificationRelation>`): A collection of wrapper objects pointing to the direct child classifications.
-   **ClassificationProperties** (`ICollection<IClassificationProperty>`): The collection of properties that are applied to this classification.
-   **Code** (`string`): A code for the classification.

## ClassificationRelation / IClassificationRelation

A helper class that encapsulates a relationship to an `IClassification` object, used for building the parent-child hierarchy.

### Properties

-   **Item** (`IClassification`): The classification object that is the target of the relation.

## ClassificationProperty / IClassificationProperty

Represents the application of a property definition (`IProperty`) to a specific classification. It adds context-specific rules like whether the property is required or has a default value.

### Properties

-   **Name** (`string`): The name of the property, delegated from the associated `PropertyAssignment`.
-   **Description** (`string`): The description of the property, delegated from the associated `PropertyAssignment`.
-   **IsRequired** (`bool`): Specifies if this property must have a value for instances of the classification.
-   **SortNumber** (`int`): A number used for ordering properties within a list.
-   **IsReadonly** (`bool`): Specifies if the property's value can be modified.
-   **DefaultValue** (`string`): A default value for this property.
-   **PropertyAssignment** (`IPropertyAssignment`): The link to the underlying global property definition.
-   **PropertySet** (`IPropertySet`): The property set this property belongs to within the context of the classification.
-   **Reference** (`string`): An external reference for this property.

## PropertySet / IPropertySet

Represents a logical grouping of properties. It inherits from `BaseClass` and primarily uses the `Name` and `Identifier` for grouping `ClassificationProperty` objects.

## Property / IProperty

An abstract base class for a reusable property definition. It defines the core attributes of a piece of data, such as its data type and unit.

### Properties

-   **StorageType** (`StorageType`): The fundamental data type of the property (e.g., `String`, `Integer`, `Boolean`).
-   **UnitAbbreviation** (`string`): The abbreviated form of the property's unit (e.g., "m", "kg").
-   **UnitType** (`string`): The type of unit for the property's value (e.g., "Length", "Mass").
-   **Code** (`string`): A code for the property.

## PropertyAssignment / IPropertyAssignment

A simple class that links a `ClassificationProperty` to its global `IProperty` definition.

### Properties

-   **Property** (`IProperty`): The referenced property definition.

## Simple Properties

-   **IPropertySimple / PropertySimple**: A property representing a simple value that can be constrained within a numeric or date range.
-   **IPropertySimpleAssignment / PropertySimpleAssignment**: The assignment version of a simple property.

### Additional Properties

-   **Max** (`string`): The maximum allowed value (inclusive or exclusive).
-   **MaxIsInclusive** (`bool`): `true` if the maximum value is allowed.
-   **Min** (`string`): The minimum allowed value (inclusive or exclusive).
-   **MinIsInclusive** (`bool`): `true` if the minimum value is allowed.

## Enumeration Properties

-   **IPropertyEnum / PropertyEnum**: A property whose value is restricted to a predefined list of choices.
-   **IPropertyEnumItem / PropertyEnumItem**: A single choice within an enumeration, consisting of a name and a dictionary of associated values.
-   **IPropertyEnumAssignment / PropertyEnumAssignment**: The assignment version, which specifies the selected enum item and whether free text is allowed.

### Properties

-   **Enums** (`IList<PropertyEnumItem>`): The list of predefined choices for the property.
-   **SelectedEnum** (`IPropertyEnumItem`): The enum item that is selected in an assignment context.
-   **FreeTextEnabled** (`bool`): If `true`, allows values other than those in the `Enums` list.

## SuperEnum & Tree Properties

These models are used for creating dependent properties, where the available options for one property depend on the value of another.

-   **IPropertyTree / PropertyTree**: A base property that uses a hierarchical `ComplexDataItem` as its data source.
-   **IPropertySuperEnum / PropertySuperEnum**: A specific type of tree property used for cascading selections, often identified by a level in the hierarchy.
-   **IPropertySuperEnumAssignment / PropertySuperEnumAssignment**: The assignment version, which links to a parent `ClassificationProperty` and can retrieve valid child values based on the parent's selection.

### Properties

-   **Item** (`ComplexDataItem`): The hierarchical data structure that defines the valid values and their relationships.
-   **Parent** (`IClassificationProperty`): In an assignment, this is the parent property that this SuperEnum depends on.
-   **Level** (`int`): The depth level in the hierarchy that this property represents.

# Complex Data & Tree Structure

## ComplexData / IComplexData

A top-level container for a collection of hierarchical data structures.

### Properties

-   **Items** (`ICollection<IComplexDataItem>`): A list of complex data items, each representing a distinct tree.

## ComplexDataItem / IComplexDataItem

Represents a single hierarchical data structure, defined by a root node. It provides methods to navigate the tree.

### Properties

-   **Path** (`string`): A string representing the path of this data structure.
-   **Root** (`IComplexDataTreeNode`): The top-level node of the tree.

## ComplexDataTreeNode / IComplexDataTreeNode

Represents a single node within a generic tree structure, containing a name, a reference to its parent, and a list of its children.

### Properties

-   **Name** (`string`): The name of the node.
-   **Children** (`IList<IComplexDataTreeNode>`): A list of child nodes.
-   **Parent** (`IComplexDataTreeNode`): A reference to the parent node.
-   **Level** (`int`): The depth of the node in the tree hierarchy.

## Mapping / IMapping

The main mapping object that connects a source system's classification and its parameters to the guideline's internal model.

### Properties

-   **SourceApplication** (`SourceSystems`): The external software or system this mapping applies to (e.g., `Revit`, `IFC`).
-   **ClassificationMap** (`IClassificationMapping`): Defines the mapping between the source and target classifications.
-   **ParameterMappings** (`ICollection<IParameterMapping>`): A collection of mappings for individual properties/parameters.

## ClassificationMapping / IClassificationMapping

Defines how a classification from a source system maps to a target `IClassification` in the guideline.

### Properties

-   **SourceClassificationName** (`string`): The name of the classification in the source system (e.g., a Revit Category).
-   **SourceClassificationParameterName** (`string`): An optional parameter name used for further filtering in the source system.
-   **SourceClassificationParameterValue** (`string`): The required value of the filter parameter.
-   **TargetClassification** (`IClassification`): The corresponding classification in the guideline.

## ParameterMapping / IParameterMapping

Defines the mapping for a single parameter (property) from a source system to a target `IClassificationProperty`.

### Properties

-   **SourceParameter** (`string`): The name of the parameter in the source system.
-   **TargetParameter** (`IClassificationProperty`): The corresponding property in the guideline.
-   **Direction** (`ParameterMappingDirection`): The direction of data flow.
-   **UsageType** (`UsageType`): Specifies if the parameter is a `Type` or `Instance` parameter.
-   **IsBuiltIn** (`bool`): `true` if the source parameter is a built-in system parameter.
-   **IsShared** (`bool`): `true` if the source parameter is a shared parameter (context-specific, e.g., in Revit).
-   **LocationParameter** (`ParameterLocation`): Specifies the location of the parameter.
-   **IsFormula** (`bool`): `true` if the value to be set is a formula.
-   **SourceParameterValue** (`string`): A specific value to be set for the parameter.

# Enums

## Direction

Defines the direction of a data mapping process.

| Member      | Description                                                    |
| :---------- | :------------------------------------------------------------- |
| `ModelToDB` | Indicates that data is flowing from the application model to the database. |
| `DBToModel` | Indicates that data is flowing from the database to the application model. |

## SourceSystems

Represents a list of known software applications from which data can originate. This enum is serializable and its members are mapped to string values (e.g., "Revit", "IFC") for data exchange.

| Member        | Description                                  |
| :------------ | :------------------------------------------- |
| `None`        | No source system is specified.               |
| `Revit`       | The data originates from Autodesk Revit.     |
| `Dynamo`      | The data originates from Autodesk Dynamo.    |
| `Rhino`       | The data originates from Rhino 3D.           |
| `IFC`         | The data is from an IFC (Industry Foundation Classes) file. |
| `Navis`       | The data originates from Autodesk Navisworks. |
| `Speckle`     | The data originates from Speckle.            |
| `Grasshopper` | The data originates from Grasshopper 3D.     |
| `AutoCAD`     | The data originates from Autodesk AutoCAD.   |
| `Civil3D`     | The data originates from Autodesk Civil 3D.  |
| `Blender`     | The data originates from Blender.            |
| `Unity`       | The data originates from the Unity engine.   |
| `Unreal`      | The data originates from the Unreal Engine.  |
| `Excel`       | The data originates from Microsoft Excel.    |
| `AIInventory` | The data originates from an AI Inventory system. |

## Status

Represents the active or inactive state of an entity.

| Member     | Description                                |
| :--------- | :----------------------------------------- |
| `Active`   | The entity is currently active and in use. |
| `Inactive` | The entity is marked as inactive or disabled. |

## StorageType

Specifies the fundamental data type for a property's value.

| Member      | Description                                  |
| :---------- | :------------------------------------------- |
| `Boolean`   | A true/false value.                          |
| `Character` | A single character.                          |
| `Integer`   | A whole number (e.g., 10, -5).               |
| `Real`      | A number with a decimal point (e.g., 3.14).  |
| `String`    | A sequence of text characters.               |
| `Time`      | A value representing a time of day.          |
| `Date`      | A value representing a calendar date.        |

## UsageType

Distinguishes whether a property applies to a type definition (template) or to a specific instance of that type.

| Member     | Description                                                           |
| :--------- | :-------------------------------------------------------------------- |
| `Type`     | The property applies to the type definition of an object (e.g., all doors of a specific model). |
| `Instance` | The property applies to an individual instance of an object (e.g., one specific door in the building). |

# DTOs

## SimpleClassificationDTO

A lightweight Data Transfer Object representing a classification with its most basic identifying information.

### Properties

-   **ID** (`string`): The unique identifier of the classification.
-   **Name** (`string`): The display name of the classification.

## ClassificationPropertyDTO

A Data Transfer Object representing a single property associated with a classification.

### Properties

-   **Id** (`string`): The unique identifier of the property.
-   **Name** (`string`): The name of the property.
-   **StorageType** (`StorageType`): An enum indicating the data type of the property (e.g., string, integer, boolean).
-   **PropertySetName** (`string`): The name of the property set to which this property belongs.
-   **PropertySetId** (`string`): The unique identifier of the property set to which this property belongs.

## PropertySetDTO

Represents a Data Transfer Object for a property set, which is a logical grouping of `ClassificationPropertyDTO`s.

### Properties

-   **Id** (`string`): The unique identifier of the property set.
-   **Name** (`string`): The name of the property set.
-   **ClassificationsProperties** (`List<ClassificationPropertyDTO>`): A list of all classification properties associated with this property set.

## ClassificationDTO

A Data Transfer Object representing a full classification, including all its associated properties. This DTO inherits from a `BaseClass`.

### Properties

-   **ClassificationProperties** (`ICollection<IClassificationProperty>`): A collection of all the properties that belong to this classification.

## BatchSearch

A class designed to hold and summarize the results of a batch search operation, such as retrieving multiple classifications by their IDs.

### Properties

-   **SuccessfulSearches** (`List<SimpleClassificationDTO>`): A list containing the DTOs for all items that were successfully found during the search.
-   **Failures** (`List<string>`): A list of the IDs that were searched for but could not be found.
-   **SuccessCount** (`int`, read-only): The total number of successful searches.
-   **FailureCount** (`int`, read-only): The total number of failed searches.
-   **TotalCount** (`int`, read-only): The total number of items that were included in the search operation.
-   **CompleteSuccess** (`bool`, read-only): A flag that is `true` if all searched items were found and at least one item was searched for.
-   **CompleteFailure** (`bool`, read-only): A flag that is `true` if none of the searched items could be found.