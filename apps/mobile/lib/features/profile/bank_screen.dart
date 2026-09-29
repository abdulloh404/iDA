import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../core/format.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../requests/request_detail_screen.dart' show FileRow;
import 'edit_request_screen.dart';
import 'profile_screen.dart' show RevealText;


class BankScreen extends StatelessWidget {
  const BankScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(s.bankTitle)),
      body: AsyncView<List<BankAccount>>(
        load: () => AppScope.read(context).repo.bankAccounts(),
        builder: (context, accounts, reload) {
          final active = accounts.where((a) => a.active).toList();
          final old = accounts.where((a) => !a.active).toList();
          return RefreshIndicator(
            onRefresh: reload,
            child: ListView(
              padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, IdaSpace.s8),
              children: [
                ContentWidth(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                    Text(s.bankForPayment, style: context.text.bodyMedium!.copyWith(color: context.ida.textSecondary)),
                    const SizedBox(height: IdaSpace.s3),
                    for (final a in active) ...[_AccountCard(a: a), const SizedBox(height: IdaSpace.s3)],
                    if (old.isNotEmpty) ...[
                      const SizedBox(height: IdaSpace.s3),
                      ExpandableSection(
                        title: s.previousAccounts,
                        icon: Icons.history_rounded,
                        initiallyExpanded: false,
                        children: [for (final a in old) _AccountDetails(a: a)],
                      ),
                    ],
                  ]),
                ),
              ],
            ),
          );
        },
      ),
      bottomNavigationBar: BottomActionBar(
        child: PrimaryButton(
          label: s.editBankTitle,
          icon: Icons.edit_note_rounded,
          onPressed: () => pushPage<void>(context, const EditBankRequestScreen()),
        ),
      ),
    );
  }
}

class _AccountCard extends StatelessWidget {
  const _AccountCard({required this.a});
  final BankAccount a;

  @override
  Widget build(BuildContext context) {
    return IdaCard(
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Row(children: [
          IconBox(Icons.account_balance_rounded, size: IdaSizes.avatarMd),
          const SizedBox(width: IdaSpace.s3),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(a.bankNameTh, style: context.text.titleSmall),
              Text(a.branchNameTh, style: context.text.bodySmall),
            ]),
          ),
          _StatusBadge(active: a.active),
        ]),
        const SizedBox(height: IdaSpace.s3),
        Divider(color: context.ida.border),
        _AccountDetails(a: a, withHeader: false),
      ]),
    );
  }
}

class _AccountDetails extends StatelessWidget {
  const _AccountDetails({required this.a, this.withHeader = true});
  final BankAccount a;
  final bool withHeader;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      if (withHeader)
        Padding(
          padding: const EdgeInsets.only(top: IdaSpace.s2),
          child: Row(children: [
            Expanded(child: Text(a.bankNameTh, style: context.text.titleSmall)),
            _StatusBadge(active: a.active),
          ]),
        ),
      KeyValue(s.expenseType, a.expenseType),
      KeyValue(s.bank, '${a.bankNameTh} (${a.bankCode})'),
      KeyValue(s.branch, '${a.branchNameTh} (${a.branchCode})'),
      KeyValue(s.accountNo, null,
          valueWidget: RevealText(masked: Fmt.maskAccount(a.accountNo), full: Fmt.formatAccount(a.accountNo))),
      KeyValue(s.accountHolder, a.accountName),
      KeyValue(s.effectiveFrom, Fmt.dateLong(a.effectiveFrom, s.en)),
      const SizedBox(height: IdaSpace.s1),
      FileRow(name: a.documentName, detail: s.viewDocument),
    ]);
  }
}

class _StatusBadge extends StatelessWidget {
  const _StatusBadge({required this.active});
  final bool active;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    return active
        ? IdaBadge(label: s.active, tone: IdaBadgeTone.success, icon: Icons.check_circle_rounded)
        : IdaBadge(label: s.inactive, tone: IdaBadgeTone.neutral, icon: Icons.remove_circle_outline_rounded);
  }
}
