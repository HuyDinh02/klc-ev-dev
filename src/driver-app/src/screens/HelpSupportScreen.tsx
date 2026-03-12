import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  TextInput,
  Alert,
  RefreshControl,
  ActivityIndicator,
  Linking,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import { Colors, Shadows } from '../constants/colors';
import { Card, Button } from '../components/common';
import { feedbackApi } from '../api/feedback';
import type { FaqItem } from '../api/feedback';

const FEEDBACK_TYPES = [
  { value: 0, labelKey: 'helpSupport.typeGeneral' },
  { value: 1, labelKey: 'helpSupport.typeBug' },
  { value: 2, labelKey: 'helpSupport.typeFeature' },
  { value: 3, labelKey: 'helpSupport.typeComplaint' },
  { value: 4, labelKey: 'helpSupport.typeCompliment' },
] as const;

export function HelpSupportScreen() {
  const { t } = useTranslation();
  const navigation = useNavigation();

  const [faqItems, setFaqItems] = useState<FaqItem[]>([]);
  const [expandedFaq, setExpandedFaq] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [feedbackType, setFeedbackType] = useState(0);
  const [subject, setSubject] = useState('');
  const [message, setMessage] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [showTypePicker, setShowTypePicker] = useState(false);

  const loadFaq = async () => {
    try {
      setError(null);
      const data = await feedbackApi.getFaq();
      setFaqItems(data);
    } catch (err) {
      console.error('Failed to load FAQ:', err);
      setError(t('errors.generic'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useFocusEffect(
    useCallback(() => {
      loadFaq();
    }, [])
  );

  const onRefresh = () => {
    setRefreshing(true);
    loadFaq();
  };

  const toggleFaq = (index: number) => {
    setExpandedFaq(expandedFaq === index ? null : index);
  };

  const handleSubmitFeedback = async () => {
    if (!subject.trim() || !message.trim()) {
      Alert.alert(t('common.error'), t('helpSupport.submitError'));
      return;
    }

    setSubmitting(true);
    try {
      await feedbackApi.submitFeedback({
        type: feedbackType,
        subject: subject.trim(),
        message: message.trim(),
      });
      Alert.alert(t('common.success'), t('helpSupport.submitSuccess'));
      setSubject('');
      setMessage('');
      setFeedbackType(0);
    } catch (err) {
      console.error('Failed to submit feedback:', err);
      Alert.alert(t('common.error'), t('helpSupport.submitError'));
    } finally {
      setSubmitting(false);
    }
  };

  const getTypeLabel = (value: number): string => {
    const type = FEEDBACK_TYPES.find((ft) => ft.value === value);
    return type ? t(type.labelKey) : t('helpSupport.typeGeneral');
  };

  if (loading) {
    return (
      <View
        style={styles.loadingContainer}
        accessible={true}
        accessibilityLabel="Loading"
        accessibilityState={{ busy: true }}
      >
        <ActivityIndicator size="large" color={Colors.primary} />
      </View>
    );
  }

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <View style={styles.header}>
        <TouchableOpacity
          onPress={() => navigation.goBack()}
          style={styles.backButton}
          accessibilityRole="button"
          accessibilityLabel={t('common.goBack')}
        >
          <Text style={styles.backButtonText}>{'\u2039'}</Text>
        </TouchableOpacity>
        <Text style={styles.headerTitle} accessibilityRole="header">
          {t('helpSupport.title')}
        </Text>
        <View style={styles.headerSpacer} />
      </View>

      <ScrollView
        style={styles.scrollView}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            tintColor={Colors.primary}
          />
        }
        keyboardShouldPersistTaps="handled"
      >
        {/* FAQ Section */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>{t('helpSupport.faq')}</Text>
          {error ? (
            <Card style={styles.errorCard}>
              <Text style={styles.errorText}>{error}</Text>
              <Button
                title={t('common.retry')}
                variant="outline"
                size="small"
                onPress={() => {
                  setLoading(true);
                  loadFaq();
                }}
                style={styles.retryButton}
              />
            </Card>
          ) : faqItems.length === 0 ? (
            <Card>
              <Text style={styles.emptyText}>{t('helpSupport.noFaq')}</Text>
            </Card>
          ) : (
            <Card padding="none">
              {faqItems.map((item, index) => (
                <View key={index}>
                  <TouchableOpacity
                    style={styles.faqItem}
                    onPress={() => toggleFaq(index)}
                    activeOpacity={0.7}
                    accessibilityRole="button"
                    accessibilityLabel={item.question}
                    accessibilityState={{ expanded: expandedFaq === index }}
                  >
                    <Text style={styles.faqQuestion}>{item.question}</Text>
                    <Text style={styles.faqChevron}>
                      {expandedFaq === index ? '\u2303' : '\u2304'}
                    </Text>
                  </TouchableOpacity>
                  {expandedFaq === index && (
                    <View style={styles.faqAnswer}>
                      <Text style={styles.faqAnswerText}>{item.answer}</Text>
                    </View>
                  )}
                  {index < faqItems.length - 1 && <View style={styles.faqDivider} />}
                </View>
              ))}
            </Card>
          )}
        </View>

        {/* Send Feedback Section */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>{t('helpSupport.sendFeedback')}</Text>
          <Card>
            {/* Feedback Type Picker */}
            <Text style={styles.inputLabel}>{t('helpSupport.feedbackType')}</Text>
            <TouchableOpacity
              style={styles.pickerButton}
              onPress={() => setShowTypePicker(!showTypePicker)}
              activeOpacity={0.7}
              accessibilityRole="button"
              accessibilityLabel={`${t('helpSupport.feedbackType')}: ${getTypeLabel(feedbackType)}`}
            >
              <Text style={styles.pickerButtonText}>{getTypeLabel(feedbackType)}</Text>
              <Text style={styles.pickerChevron}>{showTypePicker ? '\u2303' : '\u2304'}</Text>
            </TouchableOpacity>

            {showTypePicker && (
              <View style={styles.pickerOptions}>
                {FEEDBACK_TYPES.map((type) => (
                  <TouchableOpacity
                    key={type.value}
                    style={[
                      styles.pickerOption,
                      feedbackType === type.value && styles.pickerOptionSelected,
                    ]}
                    onPress={() => {
                      setFeedbackType(type.value);
                      setShowTypePicker(false);
                    }}
                    activeOpacity={0.7}
                    accessibilityRole="radio"
                    accessibilityState={{ checked: feedbackType === type.value }}
                  >
                    <Text
                      style={[
                        styles.pickerOptionText,
                        feedbackType === type.value && styles.pickerOptionTextSelected,
                      ]}
                    >
                      {t(type.labelKey)}
                    </Text>
                  </TouchableOpacity>
                ))}
              </View>
            )}

            {/* Subject Input */}
            <Text style={styles.inputLabel}>{t('helpSupport.subject')}</Text>
            <TextInput
              style={styles.textInput}
              value={subject}
              onChangeText={setSubject}
              placeholder={t('helpSupport.subjectPlaceholder')}
              placeholderTextColor={Colors.textLight}
              maxLength={200}
              accessibilityLabel={t('helpSupport.subject')}
            />

            {/* Message Input */}
            <Text style={styles.inputLabel}>{t('helpSupport.message')}</Text>
            <TextInput
              style={[styles.textInput, styles.textArea]}
              value={message}
              onChangeText={setMessage}
              placeholder={t('helpSupport.messagePlaceholder')}
              placeholderTextColor={Colors.textLight}
              multiline
              numberOfLines={5}
              textAlignVertical="top"
              maxLength={2000}
              accessibilityLabel={t('helpSupport.message')}
            />

            {/* Submit Button */}
            <Button
              title={t('helpSupport.submit')}
              onPress={handleSubmitFeedback}
              loading={submitting}
              disabled={!subject.trim() || !message.trim()}
              style={styles.submitButton}
            />
          </Card>
        </View>

        {/* Contact Info Section */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>{t('helpSupport.contactUs')}</Text>
          <Card>
            <TouchableOpacity
              style={styles.contactItem}
              onPress={() => Linking.openURL('mailto:support@klc.vn')}
              activeOpacity={0.7}
              accessibilityRole="link"
              accessibilityLabel={`${t('helpSupport.email')}: support@klc.vn`}
            >
              <Text style={styles.contactLabel}>{t('helpSupport.email')}</Text>
              <Text style={styles.contactValue}>support@klc.vn</Text>
            </TouchableOpacity>

            <View style={styles.contactDivider} />

            <TouchableOpacity
              style={styles.contactItem}
              onPress={() => Linking.openURL('tel:1900xxxx')}
              activeOpacity={0.7}
              accessibilityRole="link"
              accessibilityLabel={`${t('helpSupport.phone')}: 1900-xxxx`}
            >
              <Text style={styles.contactLabel}>{t('helpSupport.phone')}</Text>
              <Text style={styles.contactValue}>1900-xxxx</Text>
            </TouchableOpacity>

            <View style={styles.contactDivider} />

            <View style={styles.contactItem}>
              <Text style={styles.contactLabel}>{t('settings.appVersion')}</Text>
              <Text style={styles.contactValue}>1.0.0</Text>
            </View>
          </Card>
        </View>

        <View style={styles.bottomSpacer} />
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: Colors.surface,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: Colors.surface,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    backgroundColor: Colors.background,
    ...Shadows.small,
  },
  backButton: {
    width: 40,
    height: 40,
    justifyContent: 'center',
    alignItems: 'center',
  },
  backButtonText: {
    fontSize: 32,
    color: Colors.primary,
    lineHeight: 36,
  },
  headerTitle: {
    flex: 1,
    fontSize: 20,
    fontWeight: '700',
    color: Colors.text,
    textAlign: 'center',
  },
  headerSpacer: {
    width: 40,
  },
  scrollView: {
    flex: 1,
  },
  section: {
    padding: 16,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: Colors.text,
    marginBottom: 12,
  },
  errorCard: {
    alignItems: 'center',
    paddingVertical: 24,
  },
  errorText: {
    fontSize: 14,
    color: Colors.error,
    marginBottom: 12,
    textAlign: 'center',
  },
  retryButton: {
    minWidth: 100,
  },
  emptyText: {
    fontSize: 14,
    color: Colors.textSecondary,
    textAlign: 'center',
  },
  faqItem: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 14,
    paddingHorizontal: 16,
  },
  faqQuestion: {
    flex: 1,
    fontSize: 15,
    fontWeight: '600',
    color: Colors.text,
    marginRight: 12,
  },
  faqChevron: {
    fontSize: 16,
    color: Colors.textSecondary,
  },
  faqAnswer: {
    paddingHorizontal: 16,
    paddingBottom: 14,
  },
  faqAnswerText: {
    fontSize: 14,
    color: Colors.textSecondary,
    lineHeight: 20,
  },
  faqDivider: {
    height: 1,
    backgroundColor: Colors.border,
    marginHorizontal: 16,
  },
  inputLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: Colors.text,
    marginBottom: 8,
    marginTop: 16,
  },
  pickerButton: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: Colors.border,
    borderRadius: 12,
    paddingVertical: 12,
    paddingHorizontal: 16,
    backgroundColor: Colors.background,
  },
  pickerButtonText: {
    fontSize: 15,
    color: Colors.text,
  },
  pickerChevron: {
    fontSize: 14,
    color: Colors.textSecondary,
  },
  pickerOptions: {
    marginTop: 4,
    borderWidth: 1,
    borderColor: Colors.border,
    borderRadius: 12,
    backgroundColor: Colors.background,
    overflow: 'hidden',
  },
  pickerOption: {
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderBottomWidth: 1,
    borderBottomColor: Colors.border,
  },
  pickerOptionSelected: {
    backgroundColor: Colors.primary + '15',
  },
  pickerOptionText: {
    fontSize: 15,
    color: Colors.text,
  },
  pickerOptionTextSelected: {
    color: Colors.primary,
    fontWeight: '600',
  },
  textInput: {
    borderWidth: 1,
    borderColor: Colors.border,
    borderRadius: 12,
    paddingVertical: 12,
    paddingHorizontal: 16,
    fontSize: 15,
    color: Colors.text,
    backgroundColor: Colors.background,
  },
  textArea: {
    minHeight: 120,
    textAlignVertical: 'top',
  },
  submitButton: {
    marginTop: 20,
  },
  contactItem: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 12,
  },
  contactLabel: {
    fontSize: 15,
    color: Colors.textSecondary,
  },
  contactValue: {
    fontSize: 15,
    fontWeight: '600',
    color: Colors.primary,
  },
  contactDivider: {
    height: 1,
    backgroundColor: Colors.border,
  },
  bottomSpacer: {
    height: 32,
  },
});
