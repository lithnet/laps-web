﻿using System;
using System.DirectoryServices.ActiveDirectory;
using System.Linq.Expressions;
using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryForestConfigurationViewModelFactory : IActiveDirectoryForestConfigurationViewModelFactory
    {
        private readonly ICertificateProvider certificateProvider;

        private readonly IActiveDirectoryDomainConfigurationViewModelFactory domainFactory;

        private readonly IDirectory directory;

        private readonly IX509Certificate2ViewModelFactory certificate2ViewModelFactory;

        private readonly IDialogCoordinator dialogCoordinator;

        public ActiveDirectoryForestConfigurationViewModelFactory(ICertificateProvider certificateProvider, IActiveDirectoryDomainConfigurationViewModelFactory domainFactory, IDirectory directory, IX509Certificate2ViewModelFactory certificate2ViewModelFactory, IDialogCoordinator dialogCoordinator)
        {
            this.certificateProvider = certificateProvider;
            this.domainFactory = domainFactory;
            this.directory = directory;
            this.certificate2ViewModelFactory = certificate2ViewModelFactory;
            this.dialogCoordinator = dialogCoordinator;
        }

        public ActiveDirectoryForestConfigurationViewModel CreateViewModel(Forest model)
        {
            return new ActiveDirectoryForestConfigurationViewModel(model, dialogCoordinator, domainFactory, directory);
        }
    }
}