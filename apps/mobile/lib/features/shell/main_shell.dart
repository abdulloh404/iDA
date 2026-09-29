import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../account/account_screen.dart';
import '../home/home_screen.dart';
import '../notifications/notifications_screen.dart';
import '../services/services_screen.dart';


class MainShell extends StatefulWidget {
  const MainShell({super.key});

  static void goToTab(BuildContext context, int index) =>
      context.findAncestorStateOfType<_MainShellState>()?._select(index);

  @override
  State<MainShell> createState() => _MainShellState();
}

class _MainShellState extends State<MainShell> {
  int _index = 0;
  int _alertVisits = 0;

  void _select(int i) {
    setState(() {
      if (i == 2) _alertVisits++;
      _index = i;
    });
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final app = AppScope.of(context);
    final unread = app.unreadNotifications;
    return PopScope(
      canPop: _index == 0,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) _select(0);
      },
      child: Scaffold(
        body: IndexedStack(
          index: _index,
          children: [
            const HomeScreen(),
            const ServicesScreen(),
            NotificationsScreen(visit: _alertVisits),
            const AccountScreen(),
          ],
        ),
        bottomNavigationBar: DecoratedBox(
          decoration: BoxDecoration(border: Border(top: BorderSide(color: context.ida.border))),
          child: NavigationBar(
            selectedIndex: _index,
            onDestinationSelected: _select,
            destinations: [
              NavigationDestination(
                icon: const Icon(Icons.home_outlined),
                selectedIcon: const Icon(Icons.home_rounded),
                label: s.navHome,
              ),
              NavigationDestination(
                icon: const Icon(Icons.grid_view_outlined),
                selectedIcon: const Icon(Icons.grid_view_rounded),
                label: s.navServices,
              ),
              NavigationDestination(
                icon: Badge(
                  isLabelVisible: unread > 0,
                  label: Text('$unread'),
                  child: const Icon(Icons.notifications_outlined),
                ),
                selectedIcon: Badge(
                  isLabelVisible: unread > 0,
                  label: Text('$unread'),
                  child: const Icon(Icons.notifications_rounded),
                ),
                label: s.navNotifications,
                tooltip: unread > 0 ? '${s.navNotifications} · ${s.unread} $unread' : s.navNotifications,
              ),
              NavigationDestination(
                icon: const Icon(Icons.person_outline_rounded),
                selectedIcon: const Icon(Icons.person_rounded),
                label: s.navAccount,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
