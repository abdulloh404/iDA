import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../l10n/strings.dart';
import 'components.dart';

const pinLength = 6;


class PinDots extends StatefulWidget {
  const PinDots({super.key, required this.filled, this.errorTick = 0});

  final int filled;


  final int errorTick;

  @override
  State<PinDots> createState() => _PinDotsState();
}

class _PinDotsState extends State<PinDots> with SingleTickerProviderStateMixin {
  late final _shake = AnimationController(vsync: this, duration: const Duration(milliseconds: 420));

  @override
  void didUpdateWidget(covariant PinDots oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.errorTick != oldWidget.errorTick && !(MediaQuery.maybeDisableAnimationsOf(context) ?? false)) {
      _shake.forward(from: 0);
      HapticFeedback.mediumImpact();
    }
  }

  @override
  void dispose() {
    _shake.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return Semantics(
      label: '${widget.filled} / $pinLength',
      child: AnimatedBuilder(
        animation: _shake,
        builder: (context, child) => Transform.translate(
          offset: Offset(math.sin(_shake.value * math.pi * 6) * 10 * (1 - _shake.value), 0),
          child: child,
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            for (var i = 0; i < pinLength; i++)
              AnimatedContainer(
                duration: IdaMotion.of(context, IdaMotion.fast),
                margin: const EdgeInsets.symmetric(horizontal: IdaSpace.s2),
                width: IdaSizes.pinDot,
                height: IdaSizes.pinDot,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: i < widget.filled ? p.primary : Colors.transparent,
                  border: Border.all(color: i < widget.filled ? p.primary : p.borderStrong, width: 1.5),
                ),
              ),
          ],
        ),
      ),
    );
  }
}


class NumericKeypad extends StatelessWidget {
  const NumericKeypad({super.key, required this.onDigit, required this.onBackspace, this.enabled = true});

  final ValueChanged<String> onDigit;
  final VoidCallback onBackspace;
  final bool enabled;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    Widget key(String d) => _Key(
      label: d,
      onTap: enabled ? () => onDigit(d) : null,
      child: Text(d, style: idaNumeric(context.text.headlineSmall!).copyWith(fontWeight: FontWeight.w500)),
    );
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        for (final row in const [
          ['1', '2', '3'],
          ['4', '5', '6'],
          ['7', '8', '9'],
        ])
          Row(mainAxisAlignment: MainAxisAlignment.spaceEvenly, children: [for (final d in row) key(d)]),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceEvenly,
          children: [
            const SizedBox(width: IdaSizes.keypadKey, height: IdaSizes.keypadKey),
            key('0'),
            _Key(
              label: s.backspace,
              plain: true,
              onTap: enabled ? onBackspace : null,
              child: Icon(Icons.backspace_outlined, color: context.ida.textSecondary),
            ),
          ],
        ),
      ],
    );
  }
}

class _Key extends StatelessWidget {
  const _Key({required this.label, required this.child, this.onTap, this.plain = false});

  final String label;
  final Widget child;
  final VoidCallback? onTap;
  final bool plain;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: IdaSpace.s1),
      child: Semantics(
        button: true,
        label: label,
        excludeSemantics: true,
        child: Material(
          color: plain ? Colors.transparent : p.surface2,
          shape: const CircleBorder(),
          child: InkWell(
            customBorder: const CircleBorder(),
            onTap: onTap == null
                ? null
                : () {
                    HapticFeedback.selectionClick();
                    onTap!();
                  },
            child: SizedBox.square(
              dimension: IdaSizes.keypadKey,
              child: Center(child: child),
            ),
          ),
        ),
      ),
    );
  }
}


class PinPad extends StatefulWidget {
  const PinPad({super.key, required this.onCompleted, this.header, this.error, this.busy = false, this.footer});


  final Future<bool> Function(String pin) onCompleted;
  final Widget? header;
  final String? error;
  final bool busy;
  final Widget? footer;

  @override
  State<PinPad> createState() => PinPadState();
}

class PinPadState extends State<PinPad> {
  String _pin = '';
  int _errorTick = 0;

  void clear() => setState(() => _pin = '');

  Future<void> _digit(String d) async {
    if (_pin.length >= pinLength || widget.busy) return;
    setState(() => _pin += d);
    if (_pin.length == pinLength) {
      final ok = await widget.onCompleted(_pin);
      if (!mounted) return;
      if (!ok) {
        setState(() {
          _pin = '';
          _errorTick++;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return LayoutBuilder(
      builder: (context, c) {
        final compact = c.maxHeight < 560;
        return Column(
          children: [
            if (widget.header != null)
              Expanded(
                child: Center(child: SingleChildScrollView(child: widget.header!)),
              ),
            PinDots(filled: _pin.length, errorTick: _errorTick),
            SizedBox(
              height: IdaSpace.s12,
              child: Center(
                child: widget.busy
                    ? SizedBox.square(
                        dimension: IdaSizes.iconMd,
                        child: CircularProgressIndicator(strokeWidth: 2, color: p.accent),
                      )
                    : widget.error == null
                    ? null
                    : Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.error_outline_rounded, size: IdaSizes.iconSm, color: p.dangerText),
                          const SizedBox(width: IdaSpace.s1),
                          Flexible(
                            child: Text(
                              widget.error!,
                              textAlign: TextAlign.center,
                              style: context.text.bodyMedium!.copyWith(color: p.dangerText),
                            ),
                          ),
                        ],
                      ),
              ),
            ),
            Padding(
              padding: EdgeInsets.symmetric(horizontal: IdaSpace.s8, vertical: compact ? 0 : IdaSpace.s2),
              child: NumericKeypad(
                enabled: !widget.busy,
                onDigit: _digit,
                onBackspace: () {
                  if (_pin.isNotEmpty) setState(() => _pin = _pin.substring(0, _pin.length - 1));
                },
              ),
            ),
            SizedBox(height: compact ? IdaSpace.s1 : IdaSpace.s3),
            ConstrainedBox(
              constraints: const BoxConstraints(minHeight: IdaSizes.controlHeight),
              child: widget.footer ?? const SizedBox.shrink(),
            ),
            const SizedBox(height: IdaSpace.s2),
          ],
        );
      },
    );
  }
}


class OtpInput extends StatefulWidget {
  const OtpInput({
    super.key,
    required this.controller,
    required this.onCompleted,
    this.error = false,
    this.enabled = true,
  });

  final TextEditingController controller;
  final ValueChanged<String> onCompleted;
  final bool error;
  final bool enabled;

  @override
  State<OtpInput> createState() => _OtpInputState();
}

class _OtpInputState extends State<OtpInput> {
  final _focus = FocusNode();

  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_changed);
    _focus.addListener(() => setState(() {}));
  }

  void _changed() {
    setState(() {});
    if (widget.controller.text.length == pinLength) widget.onCompleted(widget.controller.text);
  }

  @override
  void dispose() {
    widget.controller.removeListener(_changed);
    _focus.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    final text = widget.controller.text;
    return LayoutBuilder(
      builder: (context, c) {

        final cellW = ((c.maxWidth / pinLength) - IdaSpace.s2).clamp(IdaSpace.s8, IdaSizes.otpCellWidth);
        return Semantics(
          label: S.of(context).otpFieldLabel,
          textField: true,
          child: Stack(
            alignment: Alignment.center,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  for (var i = 0; i < pinLength; i++)
                    AnimatedContainer(
                      duration: IdaMotion.of(context, IdaMotion.fast),
                      margin: const EdgeInsets.symmetric(horizontal: IdaSpace.s1),
                      width: cellW,
                      height: IdaSizes.otpCellHeight,
                      alignment: Alignment.center,
                      decoration: BoxDecoration(
                        color: p.surface,
                        borderRadius: IdaRadius.mdR,
                        border: Border.all(
                          width: (_focus.hasFocus && i == text.length) || widget.error ? 2 : 1,
                          color: widget.error
                              ? p.danger
                              : _focus.hasFocus && i == math.min(text.length, pinLength - 1)
                              ? p.accent
                              : i < text.length
                              ? p.primaryText
                              : p.borderStrong,
                        ),
                        boxShadow: _focus.hasFocus && i == text.length
                            ? [BoxShadow(color: p.focusRing, spreadRadius: 3)]
                            : null,
                      ),
                      child: Text(i < text.length ? text[i] : '', style: idaNumeric(context.text.headlineSmall!)),
                    ),
                ],
              ),

              Positioned.fill(
                child: Opacity(
                  opacity: 0,
                  child: TextField(
                    key: const Key('otp'),
                    controller: widget.controller,
                    focusNode: _focus,
                    autofocus: true,
                    enabled: widget.enabled,
                    keyboardType: TextInputType.number,
                    autofillHints: const [AutofillHints.oneTimeCode],
                    maxLength: pinLength,
                    showCursor: false,
                    enableInteractiveSelection: false,
                    inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                    decoration: const InputDecoration(counterText: '', border: InputBorder.none),
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}


class PasswordPolicy {
  const PasswordPolicy(this.value, this.confirm);

  final String value;
  final String confirm;

  bool get length => value.length >= 8;
  bool get upper => value.contains(RegExp(r'[A-Z]'));
  bool get lower => value.contains(RegExp(r'[a-z]'));
  bool get digit => value.contains(RegExp(r'[0-9]'));
  bool get special => value.contains(RegExp(r'[^A-Za-z0-9]'));
  bool get match => value.isNotEmpty && value == confirm;
  bool get valid => length && upper && lower && digit && special && match;
}

class PasswordRules extends StatelessWidget {
  const PasswordRules(this.policy, {super.key});
  final PasswordPolicy policy;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    Widget rule(bool ok, String text) => Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        children: [

          AnimatedSwitcher(
            duration: IdaMotion.of(context, IdaMotion.fast),
            child: Icon(
              ok ? Icons.check_circle_rounded : Icons.radio_button_unchecked_rounded,
              key: ValueKey(ok),
              size: IdaSizes.iconMd,
              color: ok ? p.success : p.borderStrong,
            ),
          ),
          const SizedBox(width: IdaSpace.s2),
          Expanded(
            child: Text(text, style: context.text.bodyMedium!.copyWith(color: ok ? p.successText : p.textSecondary)),
          ),
        ],
      ),
    );
    return Container(
      padding: const EdgeInsets.all(IdaSpace.s4),
      decoration: BoxDecoration(color: p.surface2, borderRadius: IdaRadius.lgR),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(s.passwordRulesTitle, style: context.text.labelLarge),
          const SizedBox(height: IdaSpace.s2),
          rule(policy.length, s.ruleLength),
          rule(policy.upper, s.ruleUpper),
          rule(policy.lower, s.ruleLower),
          rule(policy.digit, s.ruleDigit),
          rule(policy.special, s.ruleSpecial),
          rule(policy.match, s.ruleMatch),
        ],
      ),
    );
  }
}
