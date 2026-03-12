import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Alert,
  RefreshControl,
  ActivityIndicator,
  Modal,
  TextInput,
  KeyboardAvoidingView,
  Platform,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useFocusEffect } from '@react-navigation/native';
import { Colors, Shadows } from '../constants/colors';
import { Card, Button } from '../components/common';
import { vehiclesApi } from '../api/profile';
import type { AddVehicleRequest } from '../api/profile';
import type { Vehicle, ConnectorType } from '../types';

const CONNECTOR_OPTIONS: { label: string; value: ConnectorType }[] = [
  { label: 'Type 2', value: 'Type2' },
  { label: 'CCS2', value: 'CCS2' },
  { label: 'CHAdeMO', value: 'CHAdeMO' },
  { label: 'GB/T', value: 'GBT' },
  { label: 'Type 1', value: 'Type1' },
  { label: 'NACS', value: 'NACS' },
];

const INITIAL_FORM: AddVehicleRequest = {
  make: '',
  model: '',
  year: new Date().getFullYear(),
  licensePlate: '',
  batteryCapacityKwh: 0,
  connectorType: 'CCS2',
};

export function VehiclesScreen() {
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [modalVisible, setModalVisible] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [form, setForm] = useState<AddVehicleRequest>({ ...INITIAL_FORM });
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});

  const loadVehicles = async () => {
    try {
      setError(null);
      const data = await vehiclesApi.getAll();
      setVehicles(data);
    } catch (err) {
      console.error('Failed to load vehicles:', err);
      setError('Failed to load vehicles. Pull to refresh.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useFocusEffect(
    useCallback(() => {
      loadVehicles();
    }, [])
  );

  const onRefresh = () => {
    setRefreshing(true);
    loadVehicles();
  };

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    if (!form.make.trim()) {
      errors.make = 'Make is required';
    }
    if (!form.model.trim()) {
      errors.model = 'Model is required';
    }
    if (!form.year || form.year < 1990 || form.year > new Date().getFullYear() + 1) {
      errors.year = 'Enter a valid year';
    }
    if (!form.licensePlate.trim()) {
      errors.licensePlate = 'License plate is required';
    }
    if (!form.batteryCapacityKwh || form.batteryCapacityKwh <= 0) {
      errors.batteryCapacityKwh = 'Enter a valid battery capacity';
    }
    if (!form.connectorType) {
      errors.connectorType = 'Connector type is required';
    }

    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleAddVehicle = async () => {
    if (!validateForm()) return;

    setSubmitting(true);
    try {
      const newVehicle = await vehiclesApi.add({
        ...form,
        make: form.make.trim(),
        model: form.model.trim(),
        licensePlate: form.licensePlate.trim(),
      });
      setVehicles((prev) => [...prev, newVehicle]);
      setModalVisible(false);
      setForm({ ...INITIAL_FORM });
      setFormErrors({});
    } catch (err) {
      console.error('Failed to add vehicle:', err);
      Alert.alert('Error', 'Failed to add vehicle. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleSetDefault = async (vehicle: Vehicle) => {
    if (vehicle.isDefault) return;

    try {
      await vehiclesApi.setDefault(vehicle.id);
      setVehicles((prev) =>
        prev.map((v) => ({
          ...v,
          isDefault: v.id === vehicle.id,
        }))
      );
    } catch (err) {
      console.error('Failed to set default vehicle:', err);
      Alert.alert('Error', 'Failed to set default vehicle.');
    }
  };

  const handleDelete = (vehicle: Vehicle) => {
    Alert.alert(
      'Delete Vehicle',
      `Are you sure you want to delete ${vehicle.make} ${vehicle.model}?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Delete',
          style: 'destructive',
          onPress: async () => {
            try {
              await vehiclesApi.delete(vehicle.id);
              setVehicles((prev) => prev.filter((v) => v.id !== vehicle.id));
            } catch (err) {
              console.error('Failed to delete vehicle:', err);
              Alert.alert('Error', 'Failed to delete vehicle.');
            }
          },
        },
      ]
    );
  };

  const openAddModal = () => {
    setForm({ ...INITIAL_FORM });
    setFormErrors({});
    setModalVisible(true);
  };

  const updateForm = (field: keyof AddVehicleRequest, value: string | number) => {
    setForm((prev) => ({ ...prev, [field]: value }));
    if (formErrors[field]) {
      setFormErrors((prev) => {
        const next = { ...prev };
        delete next[field];
        return next;
      });
    }
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={Colors.primary} accessibilityLabel="Loading" />
      </View>
    );
  }

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <View style={styles.header}>
        <Text style={styles.headerTitle} accessibilityRole="header">My Vehicles</Text>
      </View>

      <ScrollView
        style={styles.scrollView}
        contentContainerStyle={styles.scrollContent}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            tintColor={Colors.primary}
          />
        }
      >
        {error ? (
          <View style={styles.errorContainer}>
            <Text style={styles.errorIcon}>!</Text>
            <Text style={styles.errorText}>{error}</Text>
            <Button
              title="Retry"
              variant="outline"
              size="small"
              onPress={() => {
                setLoading(true);
                loadVehicles();
              }}
              style={styles.retryButton}
            />
          </View>
        ) : vehicles.length === 0 ? (
          <View style={styles.emptyContainer}>
            <View style={styles.emptyIconContainer}>
              <Text style={styles.emptyIcon}>🚗</Text>
            </View>
            <Text style={styles.emptyTitle}>No vehicles added</Text>
            <Text style={styles.emptySubtitle}>
              Add your first vehicle to get personalized charging recommendations
            </Text>
            <Button
              title="Add Your First Vehicle"
              onPress={openAddModal}
              style={styles.emptyAddButton}
            />
          </View>
        ) : (
          <>
            {vehicles.map((vehicle) => (
              <TouchableOpacity
                key={vehicle.id}
                activeOpacity={0.7}
                onPress={() => handleSetDefault(vehicle)}
                accessibilityRole="button"
                accessibilityLabel={`${vehicle.make} ${vehicle.model}, ${vehicle.year}, ${vehicle.licensePlate}${vehicle.isDefault ? ', Default vehicle' : ''}`}
              >
                <Card
                  style={{
                    ...styles.vehicleCard,
                    ...(vehicle.isDefault ? styles.vehicleCardDefault : undefined),
                  }}
                >
                  <View style={styles.vehicleCardHeader}>
                    <View style={styles.vehicleMainInfo}>
                      <Text style={styles.vehicleName}>
                        {vehicle.make} {vehicle.model}
                      </Text>
                      <Text style={styles.vehicleYear}>{vehicle.year}</Text>
                    </View>
                    {vehicle.isDefault && (
                      <View style={styles.defaultBadge}>
                        <Text style={styles.defaultStar}>★</Text>
                        <Text style={styles.defaultBadgeText}>Default</Text>
                      </View>
                    )}
                  </View>

                  <View style={styles.vehicleDetails}>
                    <View style={styles.detailRow}>
                      <Text style={styles.detailLabel}>License Plate</Text>
                      <Text style={styles.detailValue}>{vehicle.licensePlate}</Text>
                    </View>
                    <View style={styles.detailRow}>
                      <Text style={styles.detailLabel}>Battery</Text>
                      <Text style={styles.detailValue}>
                        {vehicle.batteryCapacityKwh} kWh
                      </Text>
                    </View>
                    <View style={styles.detailRow}>
                      <Text style={styles.detailLabel}>Connector</Text>
                      <Text style={styles.detailValue}>{vehicle.connectorType}</Text>
                    </View>
                  </View>

                  <View style={styles.vehicleActions}>
                    {!vehicle.isDefault && (
                      <TouchableOpacity
                        style={styles.actionButton}
                        onPress={() => handleSetDefault(vehicle)}
                        accessibilityRole="button"
                        accessibilityLabel={`Set ${vehicle.make} ${vehicle.model} as default`}
                      >
                        <Text style={styles.actionButtonText}>Set as Default</Text>
                      </TouchableOpacity>
                    )}
                    <TouchableOpacity
                      style={[styles.actionButton, styles.deleteAction]}
                      onPress={() => handleDelete(vehicle)}
                      accessibilityRole="button"
                      accessibilityLabel={`Delete ${vehicle.make} ${vehicle.model}`}
                    >
                      <Text style={styles.deleteActionText}>Delete</Text>
                    </TouchableOpacity>
                  </View>
                </Card>
              </TouchableOpacity>
            ))}
          </>
        )}
      </ScrollView>

      {vehicles.length > 0 && (
        <View style={styles.fabContainer}>
          <TouchableOpacity
            style={styles.fab}
            onPress={openAddModal}
            activeOpacity={0.8}
            accessibilityRole="button"
            accessibilityLabel="Add vehicle"
          >
            <Text style={styles.fabText}>+</Text>
          </TouchableOpacity>
        </View>
      )}

      <Modal
        visible={modalVisible}
        animationType="slide"
        presentationStyle="pageSheet"
        onRequestClose={() => setModalVisible(false)}
      >
        <SafeAreaView style={styles.modalContainer} edges={['top', 'bottom']}>
          <KeyboardAvoidingView
            behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
            style={styles.modalInner}
          >
            <View style={styles.modalHeader}>
              <TouchableOpacity onPress={() => setModalVisible(false)} accessibilityRole="button" accessibilityLabel="Cancel">
                <Text style={styles.modalCancel}>Cancel</Text>
              </TouchableOpacity>
              <Text style={styles.modalTitle} accessibilityRole="header">Add Vehicle</Text>
              <View style={styles.modalHeaderSpacer} />
            </View>

            <ScrollView
              style={styles.formScroll}
              contentContainerStyle={styles.formContent}
              keyboardShouldPersistTaps="handled"
            >
              <View style={styles.formGroup}>
                <Text style={styles.formLabel}>Make *</Text>
                <TextInput
                  style={[styles.formInput, formErrors.make ? styles.formInputError : undefined]}
                  placeholder="e.g. VinFast"
                  placeholderTextColor={Colors.textLight}
                  value={form.make}
                  onChangeText={(text) => updateForm('make', text)}
                  autoCapitalize="words"
                  accessibilityLabel="Make"
                />
                {formErrors.make && (
                  <Text style={styles.formError}>{formErrors.make}</Text>
                )}
              </View>

              <View style={styles.formGroup}>
                <Text style={styles.formLabel}>Model *</Text>
                <TextInput
                  style={[styles.formInput, formErrors.model ? styles.formInputError : undefined]}
                  placeholder="e.g. VF 8"
                  placeholderTextColor={Colors.textLight}
                  value={form.model}
                  onChangeText={(text) => updateForm('model', text)}
                  autoCapitalize="words"
                  accessibilityLabel="Model"
                />
                {formErrors.model && (
                  <Text style={styles.formError}>{formErrors.model}</Text>
                )}
              </View>

              <View style={styles.formGroup}>
                <Text style={styles.formLabel}>Year *</Text>
                <TextInput
                  style={[styles.formInput, formErrors.year ? styles.formInputError : undefined]}
                  placeholder="e.g. 2025"
                  placeholderTextColor={Colors.textLight}
                  value={form.year ? String(form.year) : ''}
                  onChangeText={(text) => {
                    const num = parseInt(text, 10);
                    updateForm('year', isNaN(num) ? 0 : num);
                  }}
                  keyboardType="number-pad"
                  maxLength={4}
                  accessibilityLabel="Year"
                />
                {formErrors.year && (
                  <Text style={styles.formError}>{formErrors.year}</Text>
                )}
              </View>

              <View style={styles.formGroup}>
                <Text style={styles.formLabel}>License Plate *</Text>
                <TextInput
                  style={[
                    styles.formInput,
                    formErrors.licensePlate ? styles.formInputError : undefined,
                  ]}
                  placeholder="e.g. 30A-12345"
                  placeholderTextColor={Colors.textLight}
                  value={form.licensePlate}
                  onChangeText={(text) => updateForm('licensePlate', text)}
                  autoCapitalize="characters"
                  accessibilityLabel="License plate"
                />
                {formErrors.licensePlate && (
                  <Text style={styles.formError}>{formErrors.licensePlate}</Text>
                )}
              </View>

              <View style={styles.formGroup}>
                <Text style={styles.formLabel}>Battery Capacity (kWh) *</Text>
                <TextInput
                  style={[
                    styles.formInput,
                    !!formErrors.batteryCapacityKwh && styles.formInputError,
                  ]}
                  placeholder="e.g. 82"
                  placeholderTextColor={Colors.textLight}
                  value={form.batteryCapacityKwh ? String(form.batteryCapacityKwh) : ''}
                  onChangeText={(text) => {
                    const num = parseFloat(text);
                    updateForm('batteryCapacityKwh', isNaN(num) ? 0 : num);
                  }}
                  keyboardType="decimal-pad"
                  accessibilityLabel="Battery capacity in kilowatt hours"
                />
                {formErrors.batteryCapacityKwh && (
                  <Text style={styles.formError}>{formErrors.batteryCapacityKwh}</Text>
                )}
              </View>

              <View style={styles.formGroup}>
                <Text style={styles.formLabel}>Connector Type *</Text>
                <View style={styles.connectorOptions}>
                  {CONNECTOR_OPTIONS.map((option) => (
                    <TouchableOpacity
                      key={option.value}
                      style={[
                        styles.connectorOption,
                        form.connectorType === option.value &&
                          styles.connectorOptionSelected,
                      ]}
                      onPress={() => updateForm('connectorType', option.value)}
                      accessibilityRole="button"
                      accessibilityLabel={`Connector type ${option.label}`}
                      accessibilityState={{ selected: form.connectorType === option.value }}
                    >
                      <Text
                        style={[
                          styles.connectorOptionText,
                          form.connectorType === option.value &&
                            styles.connectorOptionTextSelected,
                        ]}
                      >
                        {option.label}
                      </Text>
                    </TouchableOpacity>
                  ))}
                </View>
                {formErrors.connectorType && (
                  <Text style={styles.formError}>{formErrors.connectorType}</Text>
                )}
              </View>

              <Button
                title="Add Vehicle"
                onPress={handleAddVehicle}
                loading={submitting}
                disabled={submitting}
                style={styles.submitButton}
              />
            </ScrollView>
          </KeyboardAvoidingView>
        </SafeAreaView>
      </Modal>
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
    paddingHorizontal: 16,
    paddingVertical: 16,
    backgroundColor: Colors.background,
    ...Shadows.small,
  },
  headerTitle: {
    fontSize: 24,
    fontWeight: '700',
    color: Colors.text,
  },
  scrollView: {
    flex: 1,
  },
  scrollContent: {
    padding: 16,
    paddingBottom: 100,
  },

  // Error state
  errorContainer: {
    alignItems: 'center',
    paddingVertical: 48,
  },
  errorIcon: {
    fontSize: 32,
    fontWeight: '700',
    color: Colors.error,
    width: 56,
    height: 56,
    lineHeight: 56,
    textAlign: 'center',
    borderRadius: 28,
    backgroundColor: Colors.error + '15',
    overflow: 'hidden',
    marginBottom: 16,
  },
  errorText: {
    fontSize: 14,
    color: Colors.textSecondary,
    textAlign: 'center',
    marginBottom: 16,
  },
  retryButton: {
    minWidth: 100,
  },

  // Empty state
  emptyContainer: {
    alignItems: 'center',
    paddingVertical: 64,
  },
  emptyIconContainer: {
    width: 96,
    height: 96,
    borderRadius: 48,
    backgroundColor: Colors.primary + '10',
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 24,
  },
  emptyIcon: {
    fontSize: 48,
  },
  emptyTitle: {
    fontSize: 20,
    fontWeight: '600',
    color: Colors.text,
    marginBottom: 8,
  },
  emptySubtitle: {
    fontSize: 14,
    color: Colors.textSecondary,
    textAlign: 'center',
    paddingHorizontal: 32,
    marginBottom: 24,
    lineHeight: 20,
  },
  emptyAddButton: {
    minWidth: 200,
  },

  // Vehicle card
  vehicleCard: {
    marginBottom: 12,
  },
  vehicleCardDefault: {
    borderWidth: 2,
    borderColor: Colors.primary,
  },
  vehicleCardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  vehicleMainInfo: {
    flex: 1,
  },
  vehicleName: {
    fontSize: 18,
    fontWeight: '600',
    color: Colors.text,
  },
  vehicleYear: {
    fontSize: 14,
    color: Colors.textSecondary,
    marginTop: 2,
  },
  defaultBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: Colors.primary + '20',
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12,
    gap: 4,
  },
  defaultStar: {
    fontSize: 12,
    color: Colors.primary,
  },
  defaultBadgeText: {
    fontSize: 12,
    fontWeight: '600',
    color: Colors.primary,
  },

  // Vehicle details
  vehicleDetails: {
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: Colors.border,
  },
  detailRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 6,
  },
  detailLabel: {
    fontSize: 13,
    color: Colors.textSecondary,
  },
  detailValue: {
    fontSize: 13,
    fontWeight: '500',
    color: Colors.text,
  },

  // Vehicle actions
  vehicleActions: {
    flexDirection: 'row',
    justifyContent: 'flex-end',
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: Colors.border,
    gap: 12,
  },
  actionButton: {
    paddingVertical: 6,
    paddingHorizontal: 14,
    borderRadius: 8,
    backgroundColor: Colors.primary + '15',
  },
  actionButtonText: {
    fontSize: 13,
    fontWeight: '600',
    color: Colors.primary,
  },
  deleteAction: {
    backgroundColor: Colors.error + '15',
  },
  deleteActionText: {
    fontSize: 13,
    fontWeight: '600',
    color: Colors.error,
  },

  // FAB
  fabContainer: {
    position: 'absolute',
    bottom: 32,
    right: 20,
  },
  fab: {
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: Colors.primary,
    justifyContent: 'center',
    alignItems: 'center',
    ...Shadows.medium,
  },
  fabText: {
    fontSize: 28,
    fontWeight: '400',
    color: Colors.background,
    marginTop: -2,
  },

  // Modal
  modalContainer: {
    flex: 1,
    backgroundColor: Colors.surface,
  },
  modalInner: {
    flex: 1,
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 16,
    backgroundColor: Colors.background,
    ...Shadows.small,
  },
  modalCancel: {
    fontSize: 16,
    color: Colors.primary,
    fontWeight: '500',
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: Colors.text,
  },
  modalHeaderSpacer: {
    width: 50,
  },

  // Form
  formScroll: {
    flex: 1,
  },
  formContent: {
    padding: 16,
    paddingBottom: 40,
  },
  formGroup: {
    marginBottom: 20,
  },
  formLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: Colors.text,
    marginBottom: 8,
  },
  formInput: {
    backgroundColor: Colors.background,
    borderWidth: 1,
    borderColor: Colors.border,
    borderRadius: 12,
    paddingHorizontal: 16,
    paddingVertical: 14,
    fontSize: 16,
    color: Colors.text,
  },
  formInputError: {
    borderColor: Colors.error,
  },
  formError: {
    fontSize: 12,
    color: Colors.error,
    marginTop: 4,
  },

  // Connector picker
  connectorOptions: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 10,
  },
  connectorOption: {
    paddingVertical: 10,
    paddingHorizontal: 18,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: Colors.border,
    backgroundColor: Colors.background,
  },
  connectorOptionSelected: {
    borderColor: Colors.primary,
    backgroundColor: Colors.primary + '15',
  },
  connectorOptionText: {
    fontSize: 14,
    fontWeight: '500',
    color: Colors.textSecondary,
  },
  connectorOptionTextSelected: {
    color: Colors.primary,
  },

  // Submit
  submitButton: {
    marginTop: 8,
  },
});
