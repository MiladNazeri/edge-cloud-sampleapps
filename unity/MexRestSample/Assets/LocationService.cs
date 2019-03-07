﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

using System;
using System.Threading.Tasks;
using DistributedMatchEngine;


public class LocationException : Exception
{
  public LocationException(string message) : base(message)
  {
  }
}

public class LocationTimeoutException : Exception
{
  public LocationTimeoutException(string message) : base(message)
  {
  }
}

// Unity Location Service
public class LocationService : MonoBehaviour
{
  // A simple UI logger.
  static StatusContainer statusContainer;

  public void Start()
  {
    statusContainer = GameObject.Find("/UICanvas/SampleOutput").GetComponent<StatusContainer>();
    statusContainer.Post("Location Services Start()");
  }

  public void Update()
  {
  }

  public static async Task<LocationInfo> UpdateLocation()
  {
    statusContainer.Post("Location Services updateLocation()");
    // First, check if user has location service enabled
    if (!Input.location.isEnabledByUser)
    {
      statusContainer.Post("Location Services Disabled");
      // Per documentation, on iOS, CoreLocation asks the user for permission.
#if UNITY_ANDROID
      // FIXME: Request permissions Unity UI for Android.
      throw new LocationException(""Location Services Disabled, cannot get location.");
#endif
    }

    // Start service before querying location
    Input.location.Start();

    // Wait until service initializes
    int maxWait = 10;
    int start = maxWait;
    while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
    {
      await Task.Delay(TimeSpan.FromSeconds(1));
      maxWait--;
    }

    statusContainer.Post("Location Services waited " + (start-maxWait));
    // Service didn't initialize in time.
    if (maxWait < 1)
    {
      print("Timed out");
      throw new LocationTimeoutException("Location Services not returning results!");
    }

    // Connection has failed
    if (Input.location.status == LocationServiceStatus.Failed)
    {
      print("Unable to determine device location");
      throw new LocationException("Location Services can't find location!");
    }
    else
    {
      // Access granted and location value could be retrieved
      statusContainer.Post("Location Services= lat: " + Input.location.lastData.latitude + ", long: " + Input.location.lastData.longitude);
      print("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
    }

    // Stop service if there is no need to query location updates continuously
    Input.location.Stop();

    LocationInfo info = Input.location.lastData;


    return info;
  }

  // Return a previously resolved location. Does not start location services.
  public static DistributedMatchEngine.Loc GetLastLocation()
  {
    return ConvertUnityLocationToDMELoc(Input.location.lastData);
  }

  // Retrieve the lastest location.
  public static async Task<DistributedMatchEngine.Loc> RetrieveLocation()
  {
    LocationInfo locationInfo = await UpdateLocation();
    return ConvertUnityLocationToDMELoc(locationInfo);
  }

  public static DistributedMatchEngine.Timestamp ConvertTimestamp(double timeInMilliseconds)
  {
    DistributedMatchEngine.Timestamp ts;

    int nanos;
    long seconds = (int)timeInMilliseconds; // Truncate.
    double remainder = timeInMilliseconds - seconds;

    nanos = (int)(remainder * 1e9);
    ts = new DistributedMatchEngine.Timestamp { seconds = seconds, nanos = nanos };
    return ts;
  }

  public static DistributedMatchEngine.Loc ConvertUnityLocationToDMELoc(UnityEngine.LocationInfo info)
  {
    DistributedMatchEngine.Timestamp ts = ConvertTimestamp(info.timestamp);

    Loc loc = new Loc
    {
      latitude = info.latitude,
      longitude = info.longitude,
      horizontal_accuracy = info.horizontalAccuracy,
      vertical_accuracy = info.verticalAccuracy,
      altitude = info.altitude,
      course = 0f,
      speed = 0f,
      timestamp = ts
    };

    return loc;
  }

}
