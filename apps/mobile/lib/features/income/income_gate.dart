import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../../ui/security_inputs.dart';


Future<void> openIncome(BuildContext context, Widget Function() page) async {
  final app = AppScope.read(context);
  if (!app.incomeUnlocked) {
    final ok = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      builder: (_) => const _IncomeGateSheet(),
    );
    if (ok != true) return;
    app.markIncomeUnlocked();
  }
  if (context.mounted) await pushPage<void>(context, page());
}

class _IncomeGateSheet extends StatefulWidget {
  const _IncomeGateSheet();

  @override
  State<_IncomeGateSheet> createState() => _IncomeGateSheetState();
}

class _IncomeGateSheetState extends State<_IncomeGateSheet> {
  String? _error;
  bool _busy = false;

  Future<bool> _check(String pin) async {
    final s = S.of(context);
    final app = AppScope.read(context);
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final r = await app.repo.verifyPin(pin);
      if (!mounted) return r.ok;
      if (r.ok) {
        Navigator.pop(context, true);
        return true;
      }
      setState(() => _error = s.pinWrong(r.attemptsLeft));
      return false;
    } on ApiException catch (e) {
      if (e.code == 'pin_locked') {
        if (mounted) Navigator.pop(context, false);
        await app.signOut();
        return false;
      }
      if (mounted) setState(() => _error = e.message);
      return false;
    } catch (e) {
      if (mounted) setState(() => _error = errorText(context, e));
      return false;
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final h = MediaQuery.sizeOf(context).height;
    return SizedBox(
      height: (h * 0.82).clamp(520.0, 720.0),
      child: SafeArea(
        top: false,
        child: PinPad(
          busy: _busy,
          error: _error,
          onCompleted: _check,
          header: Padding(
            padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s6),
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              IconBox(Icons.lock_outline_rounded, size: IdaSizes.avatarMd),
              const SizedBox(height: IdaSpace.s3),
              Text(s.incomeLockTitle, style: context.text.titleMedium, textAlign: TextAlign.center),
              const SizedBox(height: IdaSpace.s1),
              Text(s.incomeLockBody,
                  textAlign: TextAlign.center, style: context.text.bodyMedium!.copyWith(color: p.textSecondary)),
            ]),
          ),
          footer: Center(child: TextButton(onPressed: () => Navigator.pop(context, false), child: Text(s.cancel))),
        ),
      ),
    );
  }
}
