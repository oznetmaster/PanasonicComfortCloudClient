// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PanasonicComfortCloudClient.Models;

/// <summary>Identifies the device API family.</summary>
public enum DeviceFamily
	{
	/// <summary>An unrecognized or incomplete device description.</summary>
	Unknown = 0,
	/// <summary>An air-to-air unit using deviceStatus.</summary>
	AirConditioner = 1,
	/// <summary>An air-to-water heat pump using the transfer proxy.</summary>
	Aquarea = 2,
	/// <summary>A standalone hot-water unit using hphw.</summary>
	HotWater = 11,
	}

/// <summary>Reports an operation on/off value.</summary>
public enum PowerState
	{
	/// <summary>Operation is off.</summary>
	Off = 0,
	/// <summary>Operation is on.</summary>
	On = 1,
	/// <summary>The device explicitly reports an unknown operation state.</summary>
	Unknown = 2,
	}

/// <summary>Describes Aquarea status mode values; these differ from control values.</summary>
public enum AquareaReportedMode
	{
	/// <summary>Off.</summary>
	Off = 0,
	/// <summary>Heating.</summary>
	Heat = 1,
	/// <summary>Cooling.</summary>
	Cool = 2,
	/// <summary>Automatic mode currently heating.</summary>
	AutoHeat = 3,
	/// <summary>Automatic mode currently cooling.</summary>
	AutoCool = 4,
	}

/// <summary>Selects an Aquarea control mode; these differ from reported status values.</summary>
public enum AquareaMode
	{
	/// <summary>Off.</summary>
	Off = 0,
	/// <summary>Heating.</summary>
	Heat = 2,
	/// <summary>Cooling.</summary>
	Cool = 3,
	/// <summary>Automatic heating/cooling selection.</summary>
	Auto = 8,
	}

/// <summary>Selects the Aquarea quiet-operation level.</summary>
public enum QuietMode
	{
	/// <summary>Quiet mode disabled.</summary>
	Off = 0,
	/// <summary>Quiet level one.</summary>
	Level1 = 1,
	/// <summary>Quiet level two.</summary>
	Level2 = 2,
	/// <summary>Quiet level three.</summary>
	Level3 = 3,
	}

/// <summary>Selects the Aquarea powerful-mode duration.</summary>
public enum PowerfulDuration
	{
	/// <summary>Powerful mode disabled.</summary>
	Off = 0,
	/// <summary>Thirty minutes.</summary>
	Minutes30 = 1,
	/// <summary>Sixty minutes.</summary>
	Minutes60 = 2,
	/// <summary>Ninety minutes.</summary>
	Minutes90 = 3,
	}

/// <summary>Selects an Aquarea setpoint-offset mode.</summary>
public enum SpecialStatus
	{
	/// <summary>No special offset.</summary>
	None = 0,
	/// <summary>Economy offset.</summary>
	Eco = 1,
	/// <summary>Comfort offset.</summary>
	Comfort = 2,
	}

/// <summary>Selects Aquarea or HWS energy-history granularity.</summary>
public enum EnergyPeriod
	{
	/// <summary>Daily detail.</summary>
	Day = 0,
	/// <summary>Monthly detail.</summary>
	Month = 1,
	/// <summary>Yearly detail.</summary>
	Year = 2,
	}

/// <summary>Selects air-conditioner history granularity, distinct from Aquarea values.</summary>
public enum AcEnergyPeriod
	{
	/// <summary>Daily detail.</summary>
	Day = 0,
	/// <summary>Weekly detail.</summary>
	Week = 1,
	/// <summary>Monthly detail.</summary>
	Month = 2,
	/// <summary>Yearly detail.</summary>
	Year = 4,
	}

/// <summary>Selects live device status or the cloud cached status explicitly.</summary>
public enum StatusSource
	{
	/// <summary>Ask the physical device for status.</summary>
	Live = 0,
	/// <summary>Ask for the cloud cached status.</summary>
	Cached = 1,
	}

/// <summary>Selects an air-conditioner operating mode.</summary>
public enum AcMode
	{
	/// <summary>Automatic.</summary>
	Auto = 0,
	/// <summary>Dehumidification.</summary>
	Dry = 1,
	/// <summary>Cooling.</summary>
	Cool = 2,
	/// <summary>Heating.</summary>
	Heat = 3,
	/// <summary>Fan only.</summary>
	Fan = 4,
	}

/// <summary>Selects an air-conditioner fan speed.</summary>
public enum FanSpeed
	{
	/// <summary>Automatic.</summary>
	Auto = 0,
	/// <summary>Low.</summary>
	Low = 1,
	/// <summary>Low to medium.</summary>
	LowMedium = 2,
	/// <summary>Medium.</summary>
	Medium = 3,
	/// <summary>Medium to high.</summary>
	HighMedium = 4,
	/// <summary>High.</summary>
	High = 5,
	}

/// <summary>Selects an air-conditioner economy or output mode.</summary>
public enum EcoMode
	{
	/// <summary>Normal automatic operation.</summary>
	Auto = 0,
	/// <summary>Powerful operation.</summary>
	Powerful = 1,
	/// <summary>Quiet operation.</summary>
	Quiet = 2,
	}

/// <summary>Selects standalone hot-water operation.</summary>
public enum HwsMode
	{
	/// <summary>Weekly schedule.</summary>
	Schedule = 0,
	/// <summary>Photovoltaic timer.</summary>
	PhotovoltaicTimer = 1,
	/// <summary>Holiday mode.</summary>
	Holiday = 2,
	}