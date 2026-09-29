import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../data/demo_repository.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../../ui/security_inputs.dart';
import 'auth_common.dart';
import 'new_password_screen.dart';

enum _OtpPurpose { signIn, reset }


class OtpScreen extends StatefulWidget {
  const OtpScreen.signIn({super.key, required this.challenge}) : _purpose = _OtpPurpose.signIn;
  const OtpScreen.reset({super.key, required this.challenge}) : _purpose = _OtpPurpose.reset;

  final OtpChallenge challenge;
  final _OtpPurpose _purpose;

  @override
  State<OtpScreen> createState() => _OtpScreenState();
}

class _OtpScreenState extends State<OtpScreen> {
  final _code = TextEditingController();
  late OtpChallenge _challenge = widget.challenge;
  late Countdown _countdown;
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _startCountdown();
    _code.addListener(() {
      if (mounted) setState(() {});
    });
  }

  void _startCountdown() => _countdown = Countdown(_challenge.resendAfter, () {
        if (mounted) setState(() {});
      });

  @override
  void dispose() {
    _countdown.cancel();
    _code.dispose();
    super.dispose();
  }

  Future<void> _verify(String code) async {
    if (_busy) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    final app = AppScope.read(context);
    try {
      if (widget._purpose == _OtpPurpose.signIn) {
        final result = await app.repo.verifySignInOtp(_challenge.id, code);
        if (!mounted) return;
        OnboardingFlow(result).start(context);
      } else {
        final token = await app.repo.verifyResetOtp(_challenge.id, code);
        if (!mounted) return;
        Navigator.of(context).pushReplacement(MaterialPageRoute(
          builder: (_) => NewPasswordScreen(mode: PasswordMode.reset, resetToken: token),
        ));
      }
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = errorText(context, e);
        _code.clear();
      });
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _resend() async {
    final s = S.of(context);
    try {
      final c = await AppScope.read(context).repo.resendOtp(_challenge.id);
      if (!mounted) return;
      _countdown.cancel();
      setState(() {
        _challenge = c;
        _error = null;
        _code.clear();
        _startCountdown();
      });
      showToast(context, s.otpResent, tone: ToastTone.info);
    } catch (e) {
      if (mounted) showErrorToast(context, e);
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final app = AppScope.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(s.otpTitle)),
      body: SafeArea(
        child: ContentWidth(
          child: ListView(
            padding: const EdgeInsets.fromLTRB(IdaSpace.s6, IdaSpace.s8, IdaSpace.s6, IdaSpace.s6),
            children: [
              AuthHeader(
                icon: Icons.sms_outlined,
                title: s.otpTitle,
                subtitle: s.otpSentTo(_challenge.maskedPhone),
              ),
              const SizedBox(height: IdaSpace.s8),
              OtpInput(controller: _code, onCompleted: _verify, error: _error != null, enabled: !_busy),
              const SizedBox(height: IdaSpace.s3),
              Center(
                child: Text(s.otpRef(_challenge.refCode), style: idaNumeric(context.text.bodySmall!)),
              ),
              if (_error != null) ...[
                const SizedBox(height: IdaSpace.s3),
                Semantics(
                  liveRegion: true,
                  child: Row(mainAxisAlignment: MainAxisAlignment.center, children: [
                    Icon(Icons.error_outline_rounded, size: IdaSizes.iconSm, color: p.dangerText),
                    const SizedBox(width: IdaSpace.s1),
                    Flexible(child: Text(_error!, style: context.text.bodyMedium!.copyWith(color: p.dangerText))),
                  ]),
                ),
              ],
              const SizedBox(height: IdaSpace.s8),
              PrimaryButton(
                key: const Key('otp-verify'),
                label: s.otpVerify,
                loading: _busy,
                onPressed: _code.text.length == pinLength ? () => _verify(_code.text) : null,
              ),
              const SizedBox(height: IdaSpace.s3),
              Center(
                child: _countdown.done
                    ? TextButton.icon(
                        onPressed: _resend,
                        icon: const Icon(Icons.refresh_rounded, size: IdaSizes.iconMd),
                        label: Text(s.otpResend),
                      )
                    : Padding(
                        padding: const EdgeInsets.symmetric(vertical: IdaSpace.s3),
                        child: Text(s.otpResendIn(_countdown.label),
                            style: idaNumeric(context.text.bodyMedium!).copyWith(color: p.textSecondary)),
                      ),
              ),
              if (app.isDemo) ...[
                const SizedBox(height: IdaSpace.s4),
                Center(
                  child: IdaBadge(
                    label: '${s.demoMode} · OTP ${DemoRepository.demoOtp}',
                    tone: IdaBadgeTone.info,
                    icon: Icons.science_outlined,
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
