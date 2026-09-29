import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../../ui/security_inputs.dart';
import 'auth_common.dart';

enum PinMode {

  setup,


  change,


  replaceTemporary,
}

enum _Step { current, enter, confirm }

class SetPinScreen extends StatefulWidget {
  const SetPinScreen({super.key, required this.mode, this.flow, this.session});

  final PinMode mode;
  final OnboardingFlow? flow;


  final Session? session;

  @override
  State<SetPinScreen> createState() => _SetPinScreenState();
}

class _SetPinScreenState extends State<SetPinScreen> {
  final _pad = GlobalKey<PinPadState>();
  late _Step _step = widget.mode == PinMode.change ? _Step.current : _Step.enter;
  String _first = '';
  String? _error;
  bool _busy = false;

  Future<bool> _onPin(String pin) async {
    final app = AppScope.read(context);
    final s = S.of(context);
    switch (_step) {
      case _Step.current:
        setState(() => _busy = true);
        try {
          final r = await app.repo.verifyPin(pin);
          if (!r.ok) {
            setState(() => _error = s.pinWrong(r.attemptsLeft));
            return false;
          }
          _go(_Step.enter);
          return true;
        } catch (e) {
          if (mounted) setState(() => _error = errorText(context, e));
          return false;
        } finally {
          if (mounted) setState(() => _busy = false);
        }
      case _Step.enter:
        _first = pin;
        _go(_Step.confirm);
        return true;
      case _Step.confirm:
        if (pin != _first) {
          _first = '';
          _go(_Step.enter, error: s.pinMismatch);
          return false;
        }
        setState(() => _busy = true);
        try {
          await app.repo.setPin(pin);
          if (!mounted) return true;
          switch (widget.mode) {
            case PinMode.setup:
              widget.flow!.advance(context);
            case PinMode.change:
              Navigator.pop(context);
              showToast(context, s.pinChanged);
            case PinMode.replaceTemporary:
              app.enter(widget.session!);
              showToast(context, s.pinSet);
          }
          return true;
        } catch (e) {

          _first = '';
          if (mounted) _go(_Step.enter, error: errorText(context, e));
          return false;
        } finally {
          if (mounted) setState(() => _busy = false);
        }
    }
  }

  void _go(_Step step, {String? error}) {
    setState(() {
      _step = step;
      _error = error;
    });

    WidgetsBinding.instance.addPostFrameCallback((_) => _pad.currentState?.clear());
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final (title, subtitle) = switch (_step) {
      _Step.current => (s.currentPinTitle, null),
      _Step.enter => (
          widget.mode == PinMode.setup ? s.setPinTitle : s.newPinTitle,
          widget.mode == PinMode.replaceTemporary ? s.tempPinNotice : s.setPinSubtitle,
        ),
      _Step.confirm => (s.confirmPinTitle, s.confirmPinSubtitle),
    };
    final onboarding = widget.mode != PinMode.change;
    return PopScope(
      canPop: !onboarding,
      child: Scaffold(
        appBar: AppBar(
          title: Text(widget.mode == PinMode.change ? s.changePin : s.setPinTitle),
          automaticallyImplyLeading: !onboarding,
        ),
        body: SafeArea(
          child: ContentWidth(
            child: PinPad(
              key: _pad,
              busy: _busy,
              error: _error,
              onCompleted: _onPin,
              header: Padding(
                padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s6, vertical: IdaSpace.s4),
                child: Column(children: [
                  IdaLogo.mark(reversed: context.isDark),
                  const SizedBox(height: IdaSpace.s5),
                  if (widget.flow?.stepLabel(s) != null) ...[
                    Text(widget.flow!.stepLabel(s)!, style: context.text.labelMedium!.copyWith(color: p.accentStrong)),
                    const SizedBox(height: IdaSpace.s1),
                  ],
                  AnimatedSwitcher(
                    duration: IdaMotion.of(context, IdaMotion.base),
                    child: Text(title, key: ValueKey(title), style: context.text.titleLarge, textAlign: TextAlign.center),
                  ),
                  if (subtitle != null) ...[
                    const SizedBox(height: IdaSpace.s2),
                    Text(subtitle,
                        textAlign: TextAlign.center,
                        style: context.text.bodyMedium!.copyWith(color: p.textSecondary)),
                  ],
                ]),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
