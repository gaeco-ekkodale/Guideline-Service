# Introduction

This document describes the Guideline Service from a user's point of view: what a guideline
is, what it decides across the rest of the platform, and how one is put in place and
replaced.

The Guideline Service has no user interface of its own. Everything a user does with a
guideline is done through the **Platform Config** module in the Plugin Host, which is
covered in the [Platform Config user manual](https://github.com/gaeco-ekkodale/PlatformConfig).
This document explains what is happening behind that module.

# Prerequisites

- The `Guideline Server` and `Guideline Postgres` must be running.
- `Kafka` and `MiniO` must be running — a guideline is stored as a file and its contents are
  published to the other services as an event.
- The `PluginHost Service` and the `Platform Config` client must be available if you want to
  upload a guideline through the user interface.

# What a Guideline Is

A guideline is the platform's vocabulary. It defines:

- **Classifications** — the kinds of object you work with: portfolio, building, floor,
  space, air handling unit, and so on.
- **Properties** — the fields each classification carries, such as the name of a building
  or its building code.
- **Property sets** — groupings of properties, which is how the other modules decide the
  order and grouping of fields in a form.

Nothing in gaeco exists outside this vocabulary. An instance can only be created for a
classification the guideline declares, and an access right can only be granted for a
property the guideline declares. This is why uploading a guideline is the first of the three
setup steps the start page asks for.

A guideline says nothing about how objects may be *connected*. That is the ontology's job,
handled by the [Ontology Service](https://github.com/gaeco-ekkodale/OntologyService).
The two are uploaded separately and are both required before the platform is usable.

# The Guideline File

A guideline is a single file with the extension `.guideline` and JSON content. Its structure
is a `Domain` holding three collections:

```
{
  "Name": "IBPDI",
  "Version": "...",
  "Domain": {
    "Classifications": { "$values": [ ... ] },
    "Properties":      { "$values": [ ... ] },
    "PropertySets":    { "$values": [ ... ] }
  },
  "ComplexData": { ... },
  "Mappings":    { ... }
}
```

The `$id` / `$values` wrappers come from the serialiser the service uses and are part of the
expected format — a hand-written JSON array in their place will not load.

Guideline files are **not meant to be written by hand.** They are produced by the
**Guideline Editor** (the `Guideline.Editor` repository), which is where classifications and
properties are actually modelled. A ready-made example ships with the deployment
repository at `gaeco-ext/demodata/IBPDI/IBPDI.guideline`: the IBPDI Real Estate Common Data
Model, an international standard for building and real-estate information.

# Uploading a Guideline

Uploads go through Platform Config: open the **Guideline** tab and choose **+**. See
[Platform Config](https://github.com/gaeco-ekkodale/PlatformConfig)
for the walkthrough.

What happens after the file is accepted matters for what you will see next:

1. The file is stored and registered, and appears in the Guideline tab straight away.
2. Its contents are published as an event.
3. Each of the other services — Access, Instance, and the clients that read from them —
   builds its own view of the model from that event.

Step 3 is not instantaneous. A large guideline such as IBPDI takes noticeably longer than
the upload itself, and until it completes the Access Rights module has nothing to offer and
keeps its selectors disabled. If classifications have not appeared a short while after an
upload, this propagation is the thing to wait for rather than a fault to investigate.

# Replacing a Guideline

Each row in the Guideline tab offers **Replace file**, which overwrites that entry in place
rather than adding a second one.

Replacing is the operation to be careful with. If the new file no longer contains a
classification or a property, then:

- the **access rights** that referred to it are removed along with it, and
- **instances** already created under it lose the part of the model that described them.

The practical consequence: treat a guideline as stable once data exists. Adding
classifications and properties is safe; removing or renaming them is not, because the rest
of the platform refers to them by identifier. If a data model still needs restructuring,
do it before instances are created — or accept that the affected permissions have to be
configured again afterwards.

Uploading a *second* guideline instead of replacing the first is possible, and both then
coexist. The effect shows up in the Access Rights module, where each guideline contributes
its own classifications and near-identical entries end up side by side. The **Guideline**
selector there exists to scope the list to one model. Unless you deliberately want several
models on one platform, replace rather than add.

# Removing a Guideline

**Delete** on a row removes the guideline. The same cascade as replacing applies to
everything that referred to it, and there is no undo — the file has to be uploaded again.

# Where to Look When Something Is Missing

If a classification or property you expect is not offered anywhere in the platform, the
cause is almost always one of three things, in order of likelihood:

1. The guideline has not finished propagating yet — wait, then reload.
2. The guideline does not actually contain it. Download the file from the Guideline tab and
   check, rather than assuming the upload changed something.
3. Your user group has no access right for it. A property with no right is hidden rather
   than shown as locked, which is easy to mistake for a missing model. See the
   [Access Rights manual](https://github.com/gaeco-ekkodale/AccessService).

# Developer Documentation

The data model, the service architecture and the event contract are described in the
[developer documentation](../developer/01-Concepts.md).
