import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import 'menu.dart';


class ServicesScreen extends StatelessWidget {
  const ServicesScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final app = AppScope.of(context);
    final groups = <(String, List<AppMenu>)>[
      (s.groupMyInfo, [AppMenu.profile, AppMenu.bank, AppMenu.editProfile, AppMenu.editBank]),
      (s.groupWork, [AppMenu.checkIn, AppMenu.attendanceHistory, AppMenu.schedule]),
      (s.groupIncome, [AppMenu.income, AppMenu.documents]),
      (s.groupRequests, [AppMenu.pending, AppMenu.myRequests, AppMenu.history]),
    ];
    return Scaffold(
      appBar: AppBar(title: Text(s.navServices), automaticallyImplyLeading: false),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, IdaSpace.s8),
        children: [
          ContentWidth(
            child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
              for (final (title, items) in groups)
                if (items.any((m) => m.visibleTo(app))) ...[
                  SectionHeader(title),
                  IdaCard(
                    padding: const EdgeInsets.fromLTRB(IdaSpace.s2, IdaSpace.s4, IdaSpace.s2, IdaSpace.s2),
                    child: MenuGrid(items: [
                      for (final m in items.where((m) => m.visibleTo(app)))
                        MenuTile(
                          key: Key('menu-${m.id}'),
                          icon: m.icon,
                          label: m.label(s),
                          badge: m.id == 'pending' ? app.pendingApprovals : 0,
                          onTap: () => m.open(context),
                        ),
                    ]),
                  ),
                  const SizedBox(height: IdaSpace.s5),
                ],
            ]),
          ),
        ],
      ),
    );
  }
}
