import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../../ui/security_inputs.dart';
import 'auth_common.dart';

enum PasswordMode {

  firstLogin,


  change,


  reset,
}

class NewPasswordScreen extends StatefulWidget {
  const NewPasswordScreen({super.key, required this.mode, this.flow, this.resetToken});

  final PasswordMode mode;
  final OnboardingFlow? flow;
  final String? resetToken;

  @override
  State<NewPasswordScreen> createState() => _NewPasswordScreenState();
}

class _NewPasswordScreenState extends State<NewPasswordScreen> {
  final _current = TextEditingController();
  final _new = TextEditingController();
  final _confirm = TextEditingController();
  bool _busy = false;
  bool _done = false;
  String? _error;

  PasswordPolicy get _policy => PasswordPolicy(_new.text, _confirm.text);
  bool get _canSubmit =>
      _policy.valid && (widget.mode != PasswordMode.change || _current.text.isNotEmpty) && !_busy;

  @override
  void dispose() {
    _current.dispose();
    _new.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    final app = AppScope.read(context);
    final s = S.of(context);
    try {
      switch (widget.mode) {
        case PasswordMode.firstLogin:
          await app.repo.changePassword(newPassword: _new.text);
          if (mounted) widget.flow!.advance(context);
        case PasswordMode.change:
          await app.repo.changePassword(current: _current.text, newPassword: _new.text);
          if (!mounted) return;
          Navigator.pop(context);
          showToast(context, s.passwordChanged);
        case PasswordMode.reset:
          await app.repo.resetPassword(widget.resetToken!, _new.text);
          if (mounted) setState(() => _done = true);
      }
    } catch (e) {
      if (mounted) setState(() => _error = errorText(context, e));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final onboarding = widget.mode == PasswordMode.firstLogin;
    final title = widget.mode == PasswordMode.change ? s.changePassword : s.newPasswordTitle;

    if (_done) return _ResetDone(title: title);

    return PopScope(
      canPop: !onboarding,
      child: Scaffold(
        appBar: AppBar(title: Text(title), automaticallyImplyLeading: !onboarding),
        body: SafeArea(
          child: ContentWidth(
            child: ListView(
              padding: const EdgeInsets.fromLTRB(IdaSpace.s6, IdaSpace.s8, IdaSpace.s6, IdaSpace.s6),
              children: [
                AuthHeader(
                  icon: Icons.password_rounded,
                  title: title,
                  step: widget.flow?.stepLabel(s),
                  subtitle: onboarding ? s.newPasswordSubtitle : null,
                ),
                const SizedBox(height: IdaSpace.s6),
                AutofillGroup(
                  child: Column(children: [
                    if (widget.mode == PasswordMode.change) ...[
                      IdaTextField(
                        fieldKey: const Key('current-password'),
                        label: s.currentPassword,
                        controller: _current,
                        obscure: true,
                        autofillHints: const [AutofillHints.password],
                        onChanged: (_) => setState(() {}),
                      ),
                      const SizedBox(height: IdaSpace.s4),
                    ],
                    IdaTextField(
                      fieldKey: const Key('new-password'),
                      label: s.newPassword,
                      controller: _new,
                      obscure: true,
                      autofillHints: const [AutofillHints.newPassword],
                      onChanged: (_) => setState(() {}),
                    ),
                    const SizedBox(height: IdaSpace.s4),
                    IdaTextField(
                      fieldKey: const Key('confirm-password'),
                      label: s.confirmPassword,
                      controller: _confirm,
                      obscure: true,
                      autofillHints: const [AutofillHints.newPassword],
                      onChanged: (_) => setState(() {}),
                      onSubmitted: (_) {
                        if (_canSubmit) _submit();
                      },
                    ),
                  ]),
                ),
                const SizedBox(height: IdaSpace.s4),
                PasswordRules(_policy),
                if (_error != null) ...[
                  const SizedBox(height: IdaSpace.s4),
                  Row(children: [
                    Icon(Icons.error_outline_rounded, size: IdaSizes.iconSm, color: p.dangerText),
                    const SizedBox(width: IdaSpace.s1),
                    Expanded(child: Text(_error!, style: context.text.bodyMedium!.copyWith(color: p.dangerText))),
                  ]),
                ],
                const SizedBox(height: IdaSpace.s6),
                PrimaryButton(
                  key: const Key('password-submit'),
                  label: s.confirm,
                  loading: _busy,
                  onPressed: _canSubmit ? _submit : null,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _ResetDone extends StatelessWidget {
  const _ResetDone({required this.title});
  final String title;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    return Scaffold(
      appBar: AppBar(title: Text(title), automaticallyImplyLeading: false),
      body: SafeArea(
        child: ContentWidth(
          child: Padding(
            padding: const EdgeInsets.all(IdaSpace.s6),
            child: Column(children: [
              const Spacer(),
              IconBox(Icons.check_rounded, size: IdaSizes.avatarLg, bg: p.successSoft, fg: p.success),
              const SizedBox(height: IdaSpace.s5),
              Text(s.resetDone, style: context.text.headlineSmall, textAlign: TextAlign.center),
              const SizedBox(height: IdaSpace.s2),
              Text(s.resetDoneBody, style: context.text.bodyLarge!.copyWith(color: p.textSecondary), textAlign: TextAlign.center),
              const Spacer(),
              PrimaryButton(
                label: s.backToSignIn,
                onPressed: () => Navigator.of(context).popUntil((r) => r.isFirst),
              ),
            ]),
          ),
        ),
      ),
    );
  }
}
