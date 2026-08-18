// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace GuidelineService.Api.Options;

/// <summary>
/// Represents the options for connecting to Kafka.
/// </summary>
public class KafkaOptions
{
	/// <summary>
	/// Gets or sets the Kafka bootstrap server address.
	/// </summary>
	public string Address { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the username for SASL authentication.
	/// </summary>
	public string Username { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the password for SASL authentication.
	/// </summary>
	public string Password { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the Kafka topic name for uploaded guideline events.
	/// </summary>
	public string UploadedGuidelineTopic { get; set; } = string.Empty;
}
