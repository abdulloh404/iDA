import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../data/demo_repository.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../../ui/security_inputs.dart';
import 'set_pin_screen.dart';


class UnlockScreen extends StatefulWidget {
  const UnlockScreen({super.key});

  @override
  State<UnlockScreen> createState() => _UnlockScreenState();
}

class _UnlockScreenState extends State<UnlockScreen> {
  String? _error;
  bool _busy = false;

  Future<bool> _onPin(String pin) async {
    final app = AppScope.read(context);
    final s = S.of(context);
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final r = await app.repo.verifyPin(pin);
      if (!mounted) return r.ok;
      if (!r.ok) {
        setState(() => _error = s.pinWrong(r.attemptsLeft));
        return false;
      }
      if (r.temporary) {
        await pushPage<void>(context, SetPinScreen(mode: PinMode.replaceTemporary, session: app.requireSession));
        return false;
      }
      app.enter(app.requireSession);
      return true;
    } on ApiException catch (e) {

      if (e.code == 'pin_locked') {
        if (mounted) showToast(context, e.message, tone: ToastTone.error);
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

  Future<void> _forgotPin() async {
    final s = S.of(context);
    final app = AppScope.read(context);
    final ok = await confirmDialog(
      context,
      title: s.forgotPinConfirmTitle,
      body: s.forgotPinConfirmBody,
      confirmLabel: s.sendPin,
      icon: Icons.sms_outlined,
    );
    if (!ok || !mounted) return;
    try {
      final phone = await app.repo.forgotPin();
      if (!mounted) return;
      await showDialog<void>(
        context: context,
        builder: (ctx) => AlertDialog(
          icon: Center(child: IconBox(Icons.mark_chat_read_outlined, size: IdaSizes.avatarMd)),
          title: Text(s.forgotPinTitle, textAlign: TextAlign.center),
          content: Column(mainAxisSize: MainAxisSize.min, children: [
            Text(s.forgotPinBody(phone), textAlign: TextAlign.center),
            if (app.isDemo) ...[
              const SizedBox(height: IdaSpace.s3),
              IdaBadge(
                label: '${s.demoMode} · PIN ${DemoRepository.demoTempPin}',
                tone: IdaBadgeTone.info,
                icon: Icons.science_outlined,
              ),
            ],
          ]),
          actions: [FilledButton(onPressed: () => Navigator.pop(ctx), child: Text(s.close))],
        ),
      );
    } catch (e) {
      if (mounted) showErrorToast(context, e);
    }
  }

  Future<void> _switchAccount() async {
    final s = S.of(context);
    final ok = await confirmDialog(
      context,
      title: s.signOutTitle,
      body: s.signOutBody,
      confirmLabel: s.signOut,
      destructive: true,
      icon: Icons.logout_rounded,
    );
    if (ok && mounted) await AppScope.read(context).signOut();
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final app = AppScope.of(context);
    final session = app.session;
    final en = app.isEnglish;
    return Scaffold(
      body: Column(
        children: [
          const IdaBrandBar(),
          Expanded(
            child: SafeArea(
              top: true,
              child: ContentWidth(
                child: PinPad(
                  busy: _busy,
                  error: _error,
                  onCompleted: _onPin,
                  header: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s6, vertical: IdaSpace.s4),
                    child: Column(children: [
                      IdaLogo.mark(reversed: context.isDark),
                      const SizedBox(height: IdaSpace.s6),
                      if (session != null) ...[
                        IdaAvatar(session.user.initials, size: IdaSizes.avatarLg),
                        const SizedBox(height: IdaSpace.s3),
                        Text(s.hello(session.user.displayName(en)),
                            style: context.text.titleMedium, textAlign: TextAlign.center),
                        const SizedBox(height: 2),
                        Text(session.hospital.name(en),
                            style: context.text.bodyMedium!.copyWith(color: p.textSecondary)),
                      ],
                      const SizedBox(height: IdaSpace.s5),
                      Text(s.unlockTitle, style: context.text.bodyLarge!.copyWith(color: p.textSecondary)),
                    ]),
                  ),
                  footer: Wrap(
                    alignment: WrapAlignment.center,
                    spacing: IdaSpace.s2,
                    children: [
                      TextButton(key: const Key('forgot-pin'), onPressed: _forgotPin, child: Text(s.forgotPin)),
                      TextButton(onPressed: _switchAccount, child: Text(s.switchAccount)),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
