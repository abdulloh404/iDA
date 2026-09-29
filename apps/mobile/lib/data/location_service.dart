import 'package:geolocator/geolocator.dart';

import 'models.dart';


sealed class LocationResult {
  const LocationResult();
}

class LocationOk extends LocationResult {
  const LocationOk(this.point);
  final GeoPoint point;
}


class LocationDenied extends LocationResult {
  const LocationDenied();
}


class LocationDeniedForever extends LocationResult {
  const LocationDeniedForever();
}


class LocationServiceOff extends LocationResult {
  const LocationServiceOff();
}

class LocationFailed extends LocationResult {
  const LocationFailed(this.message);
  final String message;
}

abstract interface class LocationService {
  Future<LocationResult> current();
  Future<void> openSettings();
}


class DeviceLocationService implements LocationService {
  @override
  Future<LocationResult> current() async {
    try {
      if (!await Geolocator.isLocationServiceEnabled()) return const LocationServiceOff();
      var permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }
      if (permission == LocationPermission.denied) return const LocationDenied();
      if (permission == LocationPermission.deniedForever) return const LocationDeniedForever();
      final pos = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: Duration(seconds: 15),
        ),
      );
      return LocationOk(GeoPoint(pos.latitude, pos.longitude, accuracyM: pos.accuracy));
    } catch (e) {
      return LocationFailed('$e');
    }
  }

  @override
  Future<void> openSettings() async {
    if (!await Geolocator.isLocationServiceEnabled()) {
      await Geolocator.openLocationSettings();
    } else {
      await Geolocator.openAppSettings();
    }
  }
}


class DemoLocationService implements LocationService {
  DemoLocationService(this._point, {this.latency = const Duration(milliseconds: 600)});

  final GeoPoint Function() _point;
  final Duration latency;

  @override
  Future<LocationResult> current() async {
    if (latency > Duration.zero) await Future<void>.delayed(latency);
    return LocationOk(_point());
  }

  @override
  Future<void> openSettings() async {}
}
