import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'app/app_controller.dart';
import 'data/demo_repository.dart';
import 'data/location_service.dart';
import 'features/auth/login_screen.dart';
import 'features/auth/splash_screen.dart';
import 'features/auth/unlock_screen.dart';
import 'features/shell/main_shell.dart';
import 'theme/ida_theme.dart';


const _gps = String.fromEnvironment('IDA_GPS', defaultValue: 'demo');

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final prefs = await SharedPreferences.getInstance();


  final repo = DemoRepository(prefs);
  final LocationService location =
      _gps == 'device' ? DeviceLocationService() : DemoLocationService(repo.demoDeviceLocation);

  final controller = AppController(repo: repo, location: location, prefs: prefs, isDemo: true);
  runApp(IdaApp(controller: controller));
  await controller.boot();
}

class IdaApp extends StatelessWidget {
  const IdaApp({super.key, required this.controller});
  final AppController controller;

  @override
  Widget build(BuildContext context) => AppScope(
        controller: controller,
        child: Builder(builder: (context) {
          final app = AppScope.of(context);
          return MaterialApp(
            title: 'iDA',
            debugShowCheckedModeBanner: false,
            navigatorKey: app.navigatorKey,
            scaffoldMessengerKey: app.messengerKey,

            theme: idaLightTheme,
            darkTheme: idaDarkTheme,
            themeMode: app.themeMode,
            locale: app.locale,
            supportedLocales: const [Locale('th'), Locale('en')],
            localizationsDelegates: GlobalMaterialLocalizations.delegates,

            builder: (context, child) => MediaQuery.withClampedTextScaling(maxScaleFactor: 1.5, child: child!),
            home: AnimatedSwitcher(
              duration: IdaMotion.of(context, IdaMotion.slow),
              child: switch (app.stage) {
                AuthStage.booting => const SplashScreen(key: ValueKey('boot')),
                AuthStage.signedOut => const LoginScreen(key: ValueKey('login')),
                AuthStage.locked => const UnlockScreen(key: ValueKey('lock')),
                AuthStage.ready => const MainShell(key: ValueKey('shell')),
              },
            ),
          );
        }),
      );
}
