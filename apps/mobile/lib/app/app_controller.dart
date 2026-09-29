import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../data/location_service.dart';
import '../data/models.dart';
import '../data/repository.dart';


enum AuthStage {

  booting,


  signedOut,


  locked,
  ready,
}


class AppController extends ChangeNotifier with WidgetsBindingObserver {
  AppController({
    required this.repo,
    required this.location,
    required this._prefs,
    this.isDemo = false,
    this.lockAfter = const Duration(minutes: 5),
    this.incomeUnlockFor = const Duration(minutes: 10),
  }) {
    _locale = Locale(_prefs.getString(_kLocale) ?? 'th');
    _themeMode = ThemeMode.values.firstWhere(
      (m) => m.name == _prefs.getString(_kTheme),
      orElse: () => ThemeMode.system,
    );
    _rememberedUsername = _prefs.getString(_kUsername);
  }

  static const _kLocale = 'ida.locale';
  static const _kTheme = 'ida.theme';
  static const _kUsername = 'ida.username';

  final IdaRepository repo;
  final LocationService location;
  final SharedPreferences _prefs;
  final bool isDemo;


  final Duration lockAfter;


  final Duration incomeUnlockFor;

  final navigatorKey = GlobalKey<NavigatorState>();
  final messengerKey = GlobalKey<ScaffoldMessengerState>();

  AuthStage _stage = AuthStage.booting;
  AuthStage get stage => _stage;

  Session? _session;
  Session? get session => _session;
  Session get requireSession => _session!;

  late Locale _locale;
  Locale get locale => _locale;
  bool get isEnglish => _locale.languageCode == 'en';

  late ThemeMode _themeMode;
  ThemeMode get themeMode => _themeMode;

  String? _rememberedUsername;
  String? get rememberedUsername => _rememberedUsername;


  bool _amountsHidden = true;
  bool get amountsHidden => _amountsHidden;

  DateTime? _incomeUnlockedAt;
  bool get incomeUnlocked =>
      _incomeUnlockedAt != null && DateTime.now().difference(_incomeUnlockedAt!) < incomeUnlockFor;

  int _unread = 0;
  int get unreadNotifications => _unread;
  int _pending = 0;
  int get pendingApprovals => _pending;


  int _dataVersion = 0;
  int get dataVersion => _dataVersion;

  DateTime? _pausedAt;

  Future<void> boot() async {
    WidgetsBinding.instance.addObserver(this);
    try {
      _session = await repo.restoreSession();
    } catch (_) {
      _session = null;
    }
    _setStage(_session == null ? AuthStage.signedOut : AuthStage.locked);
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }


  void setLocale(Locale locale) {
    if (locale == _locale) return;
    _locale = locale;
    _prefs.setString(_kLocale, locale.languageCode);
    notifyListeners();
  }

  void toggleLocale() => setLocale(Locale(isEnglish ? 'th' : 'en'));

  void setThemeMode(ThemeMode mode) {
    if (mode == _themeMode) return;
    _themeMode = mode;
    _prefs.setString(_kTheme, mode.name);
    notifyListeners();
  }

  void rememberUsername(String? username) {
    _rememberedUsername = username;
    if (username == null) {
      _prefs.remove(_kUsername);
    } else {
      _prefs.setString(_kUsername, username);
    }
  }

  void toggleAmounts() {
    _amountsHidden = !_amountsHidden;
    notifyListeners();
  }


  void markIncomeUnlocked() {
    _incomeUnlockedAt = DateTime.now();
    _amountsHidden = false;
    notifyListeners();
  }


  void enter(Session session) {
    _session = session;
    _setStage(AuthStage.ready);
    refreshBadges();
  }

  void lock() {
    if (_stage != AuthStage.ready) return;
    _incomeUnlockedAt = null;
    _amountsHidden = true;
    _setStage(AuthStage.locked);
  }

  Future<void> signOut() async {
    try {
      await repo.signOut();
    } finally {
      _session = null;
      _incomeUnlockedAt = null;
      _amountsHidden = true;
      _unread = 0;
      _pending = 0;
      _setStage(AuthStage.signedOut);
    }
  }

  Future<void> switchHospital(String hospitalId) async {
    _session = await repo.switchHospital(hospitalId);
    _incomeUnlockedAt = null;
    _dataVersion++;
    notifyListeners();
    await refreshBadges();
  }

  Future<void> refreshBadges() async {
    if (_session == null) return;
    try {
      final list = await repo.notifications();
      _unread = list.where((n) => !n.read).length;
      _pending = _session!.canApprove ? (await repo.pendingApprovals()).length : 0;
      notifyListeners();
    } catch (_) {

    }
  }

  void _setStage(AuthStage stage) {
    _stage = stage;

    navigatorKey.currentState?.popUntil((r) => r.isFirst);
    notifyListeners();
  }


  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    switch (state) {
      case AppLifecycleState.paused:
      case AppLifecycleState.hidden:
        _pausedAt ??= DateTime.now();
      case AppLifecycleState.resumed:
        final at = _pausedAt;
        _pausedAt = null;
        if (at != null && DateTime.now().difference(at) >= lockAfter) lock();
      default:
        break;
    }
  }
}


class AppScope extends InheritedNotifier<AppController> {
  const AppScope({super.key, required AppController controller, required super.child})
      : super(notifier: controller);


  static AppController of(BuildContext context) =>
      context.dependOnInheritedWidgetOfExactType<AppScope>()!.notifier!;


  static AppController read(BuildContext context) =>
      context.getInheritedWidgetOfExactType<AppScope>()!.notifier!;
}
