using System;
using System.Diagnostics;
using System.Linq;
using Google.Protobuf;
using POGOProtos.Networking.Envelopes;
using static POGOProtos.Networking.Envelopes.Signature.Types;

namespace PokemonGoApi.Source.Helpers
{
	public class EncryptionHelper
	{
		private readonly Stopwatch _internalStopwatch;

		private readonly bool _is64Bit;

		private readonly Random _random;

		internal EncryptionHelper()
		{
			_internalStopwatch = new Stopwatch();
			_is64Bit = Environment.Is64BitOperatingSystem;
			_random = new Random();
		}

		/// <summary>
		///     Generates the encrypted signature which is required for the <see cref="RequestEnvelope"/>.
		/// </summary>
		/// <param name="requests">The requests of the <see cref="RequestEnvelope"/>.</param>
		/// <returns>The encrypted <see cref="Unknown6"/> (Signature).</returns>
		internal Unknown6 GenerateSignature(RequestEnvelope requestEnvelope, double lat, double lng)
		{
			var signature = new Signature
			{
				TimestampSinceStart = (ulong)_internalStopwatch.ElapsedMilliseconds,
				Timestamp = (ulong)TimeUtil.GetCurrentTimestampInMilliseconds(),
				SensorInfo = new SensorInfo()
				{
					AccelNormalizedZ = Randomize(9.8),
					AccelNormalizedX = Randomize(0.02),
					AccelNormalizedY = Randomize(0.3),
					TimestampSnapshot = (ulong)_internalStopwatch.ElapsedMilliseconds - 230,
					MagnetometerX = Randomize(012271042913198471),
					MagnetometerY = Randomize(-0.015570580959320068),
					MagnetometerZ = Randomize(0.010850906372070313),
					AngleNormalizedX = Randomize(17.950439453125),
					AngleNormalizedY = Randomize(-23.36273193359375),
					AngleNormalizedZ = Randomize(-48.8250732421875),
					AccelRawX = Randomize(-0.0120010357350111),
					AccelRawY = Randomize(-0.04214850440621376),
					AccelRawZ = Randomize(0.94571763277053833),
					GyroscopeRawX = Randomize(7.62939453125e-005),
					GyroscopeRawY = Randomize(-0.00054931640625),
					GyroscopeRawZ = Randomize(0.0024566650390625),
					AccelerometerAxes = 3
				},
				DeviceInfo = new DeviceInfo()
				{
					DeviceId = "8525f5d8201f78b5",
					AndroidBoardName = "hammerhead",
					AndroidBootloader = "HHZ20b",
					DeviceBrand = "google",
					DeviceModel = "Nexus 5",
					DeviceModelIdentifier = "hammerhead",
					DeviceModelBoot = "qcom",
					HardwareManufacturer = "LGE",
					HardwareModel = "Nexus 5",
					FirmwareBrand = "hammerhead",
					FirmwareTags = "release-keys",
					FirmwareType = "user",
					FirmwareFingerprint = "google/hammerhead/hammerhead:6.0.1/MOB30M/2862625:user/release-keys"
				},
				LocationFix =
				{
					new LocationFix
					{
						Provider = "network",
                        //Unk4 = 120,
                        Latitude = (float)lat,
						Longitude = (float)lng,
						Altitude = (float)100,
						TimestampSinceStart = (ulong)_internalStopwatch.ElapsedMilliseconds - 200,
						Floor = 3,
						LocationType = 1
					}
				}
			};

			// Compute 10
			var serializedTicket = requestEnvelope.AuthTicket != null ? requestEnvelope.AuthTicket.ToByteArray() : requestEnvelope.AuthInfo.ToByteArray();
			var firstHash = CalculateHash32(serializedTicket, 0x1B845238);
			var locationBytes = BitConverter.GetBytes(lat).Reverse()
				.Concat(BitConverter.GetBytes(lng).Reverse())
				.Concat(BitConverter.GetBytes(100).Reverse()).ToArray();
			signature.LocationHash1 = CalculateHash32(locationBytes, firstHash);
			// Compute 20
			signature.LocationHash2 = CalculateHash32(locationBytes, 0x1B845238);
			// Compute 24
			var seed = xxHash64.CalculateHash(serializedTicket, serializedTicket.Length, 0x1B845238);
			foreach (var req in requestEnvelope.Requests)
			{
				var reqBytes = req.ToByteArray();
				signature.RequestHash.Add(xxHash64.CalculateHash(reqBytes, reqBytes.Length, seed));
			}

			//static for now
			signature.Unknown22 = ByteString.CopyFrom(0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F);

			var iv = new byte[32];
			_random.NextBytes(iv);

			var encryptedSignature = new Unknown6
			{
				RequestType = 6,
				Unknown2 = new Unknown6.Types.Unknown2
				{
					EncryptedSignature = ByteString.CopyFrom(CryptUtil.Encrypt(signature.ToByteArray(), iv))
				}
			};

			return encryptedSignature;
		}

		private uint CalculateHash32(byte[] bytes, uint seed)
		{
			var xxHash = new xxHash32();
			xxHash.Init(seed);
			xxHash.Update(bytes, bytes.Length);
			return xxHash.Digest();
		}

		private double Randomize(double num)
		{
			var randomFactor = 0.3f;
			var randomMin = (num * (1 - randomFactor));
			var randomMax = (num * (1 + randomFactor));
			var randomizedDelay = new Random().NextDouble() * (randomMax - randomMin) + randomMin; ;
			return randomizedDelay; ;
		}

	}
}